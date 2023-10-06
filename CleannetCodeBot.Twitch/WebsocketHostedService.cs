using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.Api.Helix.Models.Subscriptions;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using TwitchLib.EventSub.Websockets.Core.EventArgs.Channel;

namespace CleannetCodeBot.Twitch;

public class WebsocketHostedService : BackgroundService
{
    private readonly ILogger<WebsocketHostedService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly AppSettings _appSettings;
    private readonly EventSubWebsocketClient _eventSubWebsocketClient;

    public WebsocketHostedService(
        ILogger<WebsocketHostedService> logger,
        IMemoryCache memoryCache,
        IOptions<AppSettings> options,
        EventSubWebsocketClient eventSubWebsocketClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryCache = memoryCache;
        _appSettings = options.Value;

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
        while (!cancellationToken.IsCancellationRequested)
        {
            var authToken = _memoryCache.Get<AuthToken>(AuthToken.Key);
            if (authToken == null)
            {
                _logger.LogError("user code not found");
                await Task.Delay(5_000, cancellationToken);
                continue;
            }

            await _eventSubWebsocketClient.ConnectAsync();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _eventSubWebsocketClient.DisconnectAsync();
    }

    private void OnWebsocketConnected(object? sender, WebsocketConnectedArgs e)
    {
        var task = Task.Run(async () =>
        {
            _logger.LogInformation($"Websocket {_eventSubWebsocketClient.SessionId} connected!");

            if (!e.IsRequestedReconnect)
            {
                var authToken = _memoryCache.Get<AuthToken>(AuthToken.Key);
                if (authToken == null)
                {
                    throw new Exception("user code not found");
                }

                var api = new TwitchAPI
                {
                    Settings =
                    {
                        ClientId = _appSettings.ClientId,
                        AccessToken = authToken.AccessToken
                    }
                };

                // subscribe to topics
                var subs = await api.Helix.EventSub.GetEventSubSubscriptionsAsync();
                _logger.LogInformation($"Subscriptions: {subs.Total}");

                // var getUsersResponse = await api.Helix.Users.GetUsersAsync(logins: new List<string> { "cleannetcode" });
                // var user = getUsersResponse.Users.FirstOrDefault();

                // if (user == null)
                // {
                // throw new Exception("User not found");
                // }

                await api.Helix.EventSub.CreateEventSubSubscriptionAsync(
                    type: "channel.channel_points_custom_reward_redemption.add",
                    version: "1",
                    condition: new Dictionary<string, string>
                    {
                        { "broadcaster_user_id", "597719687" }
                    },
                    method: EventSubTransportMethod.Websocket,
                    accessToken: authToken.AccessToken,
                    websocketSessionId: _eventSubWebsocketClient.SessionId);

                subs = await api.Helix.EventSub.GetEventSubSubscriptionsAsync();
                _logger.LogInformation($"Subscriptions: {subs.Total}");
            }
        });

        task.ContinueWith(
            x => x.Exception?
                .Flatten()
                .Handle(ex =>
                {
                    _logger.LogError(ex, "Error occurred while connecting to websocket");
                    return true;
                }),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    private async void OnWebsocketDisconnected(object? sender, EventArgs e)
    {
        _logger.LogError($"Websocket {_eventSubWebsocketClient.SessionId} disconnected!");

        // Don't do this in production. You should implement a better reconnect strategy with exponential backoff
        while (!await _eventSubWebsocketClient.ReconnectAsync())
        {
            _logger.LogError("Websocket reconnect failed!");
            await Task.Delay(1000);
        }
    }

    private void OnWebsocketReconnected(object? sender, EventArgs e)
    {
        _logger.LogWarning($"Websocket {_eventSubWebsocketClient.SessionId} reconnected");
    }

    private void OnErrorOccurred(object? sender, ErrorOccuredArgs e)
    {
        _logger.LogError($"Websocket {_eventSubWebsocketClient.SessionId} - Error occurred!");
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