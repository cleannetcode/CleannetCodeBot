using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Interfaces;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace CleannetCodeBot.Twitch;

public class TwitchWebsocketBackgroundService : BackgroundService
{
    private readonly ILogger<TwitchWebsocketBackgroundService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly ITwitchAPI _twitchApi;
    private readonly AppSettings _appSettings;
    private readonly EventSubWebsocketClient _eventSubWebsocketClient;
    private readonly ResiliencePipeline _resiliencePipeline;
    private const string ChannelRewardRedemption = "channel.channel_points_custom_reward_redemption.add";

    public TwitchWebsocketBackgroundService(
        ILogger<TwitchWebsocketBackgroundService> logger,
        ResiliencePipelineProvider<string> resiliencePipelineProvider,
        IMemoryCache memoryCache,
        ITwitchAPI twitchApi,
        IOptions<AppSettings> options,
        EventSubWebsocketClient eventSubWebsocketClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryCache = memoryCache;
        _twitchApi = twitchApi;
        _appSettings = options.Value;
        _resiliencePipeline = resiliencePipelineProvider.GetPipeline(ServiceCollectionExtension.RetryResiliencePipeline);

        _eventSubWebsocketClient = eventSubWebsocketClient ?? throw new ArgumentNullException(nameof(eventSubWebsocketClient));
        _eventSubWebsocketClient.WebsocketConnected += OnWebsocketConnected;
        _eventSubWebsocketClient.WebsocketDisconnected += OnWebsocketDisconnected;
        _eventSubWebsocketClient.WebsocketReconnected += OnWebsocketReconnected;
        _eventSubWebsocketClient.ErrorOccurred += OnErrorOccurred;

        _eventSubWebsocketClient.ChannelFollow += OnChannelFollow;
        _eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionAdd += OnChannelPointsCustomRewardRedemptionAdd;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var isConnected = false;

        while (!cancellationToken.IsCancellationRequested && !isConnected)
        {
            var authToken = _memoryCache.Get<AuthToken>(AuthToken.Key);
            if (authToken == null)
            {
                _logger.LogError("user code not found");
                await Task.Delay(5_000, cancellationToken);
                continue;
            }

            var authTokenResponse = await _twitchApi.Auth.ValidateAccessTokenAsync(authToken.AccessToken);
            if (authTokenResponse == null)
            {
                _memoryCache.Remove(AuthToken.Key);
                continue;
            }

            _twitchApi.Settings.AccessToken = authToken.AccessToken;

            isConnected = await _eventSubWebsocketClient.ConnectAsync();
        }

        while (!cancellationToken.IsCancellationRequested && isConnected)
        {
            _logger.LogDebug("Twitch bot is running");
            await Task.Delay(5_000, cancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _eventSubWebsocketClient.DisconnectAsync();
    }

    private void OnWebsocketConnected(object? sender, WebsocketConnectedArgs e)
    {
        _logger.LogInformation($"Websocket {_eventSubWebsocketClient.SessionId} connected!");

        if (e.IsRequestedReconnect) return;

        Task.Run(async () =>
        {
            try
            {
                var authTokenResponse = await _twitchApi.Auth.ValidateAccessTokenAsync();

                var subscriptionsResponse = await _twitchApi.Helix.EventSub.GetEventSubSubscriptionsAsync();
                foreach (var subscription in subscriptionsResponse.Subscriptions)
                {
                    await _twitchApi.Helix.EventSub.DeleteEventSubSubscriptionAsync(subscription.Id);
                }

                await _twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync(
                    type: ChannelRewardRedemption,
                    version: "1",
                    condition: new Dictionary<string, string>
                    {
                        { "broadcaster_user_id", authTokenResponse.UserId }
                    },
                    method: EventSubTransportMethod.Websocket,
                    websocketSessionId: _eventSubWebsocketClient.SessionId);

                _logger.LogInformation("Subscriptions: {subscriptionTypes}", ChannelRewardRedemption);
            }
            catch (Exception ex)
            {
                var isAuthException = ex switch
                {
                    BadScopeException => true,
                    BadTokenException => true,
                    InvalidCredentialException => true,
                    TokenExpiredException => true,
                    _ => false
                };

                if (isAuthException)
                {
                    _memoryCache.Remove(AuthToken.Key);
                }
                else
                {
                    throw;
                }
            }
        });
    }

    private void OnWebsocketDisconnected(object? sender, EventArgs e)
    {
        _logger.LogError($"Websocket {_eventSubWebsocketClient.SessionId} disconnected!");

        _resiliencePipeline.ExecuteAsync(
            async (state, _) => await state.ReconnectAsync(),
            _eventSubWebsocketClient);
    }

    private void OnWebsocketReconnected(object? sender, EventArgs e)
    {
        _logger.LogWarning($"Websocket {_eventSubWebsocketClient.SessionId} reconnected");
    }

    private void OnErrorOccurred(object? sender, ErrorOccuredArgs e)
    {
        _logger.LogError(e.Exception.Message, e.Exception);

        if (e.Exception is TokenExpiredException)
        {
            var authToken = _memoryCache.Get<AuthToken>(AuthToken.Key);
            if (authToken != null)
            {
                _twitchApi.Auth.RefreshAuthTokenAsync(authToken.RefreshToken, _appSettings.ClientSecret)
                    .ContinueWith(x =>
                    {
                        _memoryCache.Set(
                            AuthToken.Key,
                            new AuthToken(x.Result.AccessToken, x.Result.ExpiresIn, x.Result.RefreshToken));

                        _twitchApi.Settings.AccessToken = x.Result.AccessToken;
                    });
            }
        }
    }

    private void OnChannelFollow(object? sender, ChannelFollowArgs e)
    {
        var eventData = e.Notification.Payload.Event;
        _logger.LogInformation($"{eventData.UserName} followed {eventData.BroadcasterUserName} at {eventData.FollowedAt}");
    }

    private void OnChannelPointsCustomRewardRedemptionAdd(object? sender, ChannelPointsCustomRewardRedemptionArgs e)
    {
        var eventData = e.Notification.Payload.Event;
        _logger.LogInformation($"{eventData.UserName} requested from {eventData.BroadcasterUserName} reward {eventData.Reward.Title}");
    }
}