using System.Text;
using CleannetCodeBot.Twitch.Controllers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace CleannetCodeBot.Twitch;

public class TwitchBotBackgroundService : BackgroundService
{
    private readonly TwitchClient _client;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<TwitchBotBackgroundService> _logger;
    private readonly AppSettings _appSettings;

    public TwitchBotBackgroundService(
        TwitchClient twitchApiClient,
        IMemoryCache memoryCache,
        IOptions<AppSettings> options,
        ILogger<TwitchBotBackgroundService> logger)
    {
        _client = twitchApiClient;
        _memoryCache = memoryCache;
        _appSettings = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var authToken = _memoryCache.Get<AuthToken>(AuthToken.Key);
            if (authToken == null)
            {
                _logger.LogInformation("user code not found");
                await Task.Delay(5_000, cancellationToken);
                continue;
            }

            var credentials = new ConnectionCredentials("cleannetcode", authToken.AccessToken);
            _client.OnLog += Client_OnLog;
            _client.OnMessageReceived += Client_OnMessageReceived;
            _client.OnConnected += Client_OnConnected;
            _client.Initialize(credentials, "cleannetcode");
            _client.Connect();

            while (!cancellationToken.IsCancellationRequested && _client.IsConnected)
            {
                _logger.LogDebug("Twitch bot is running");
                await Task.Delay(5_000, cancellationToken);
            }
        }
    }

    private void Client_OnLog(object sender, OnLogArgs e)
    {
        _logger.LogInformation($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
    }

    private void Client_OnConnected(object sender, OnConnectedArgs e)
    {
        _logger.LogInformation($"Connected to {e.AutoJoinChannel}");
    }

    private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
    {
        if (e.ChatMessage.Message.StartsWith("!content"))
        {
            _client.SendMessage(e.ChatMessage.Channel, "lolkek cheburek");
        }

        var boostyMessage = "Доступ до закрытого дополнительного контента (записи ВИП стримов, ВИП стримы, Индивидуальные занятия, Тестирование знаний, Дополнительные статьи о том о сем и мой мини блог про мою текущую работу) https://boosty.to/cleannetcode";

        if (e.ChatMessage.Message.StartsWith("!boosty") || e.ChatMessage.Message.StartsWith("!бусти"))
        {
            _client.SendMessage(e.ChatMessage.Channel, boostyMessage);
        }
        else if (e.ChatMessage.Message.StartsWith("!tg"))
        {
            var tgMessage = "Чат для общения с участниками нашего комьюнити. Тут можно найти ответы на разные вопросы, мемы, друзей и поддержку :) https://t.me/cleannetcode";
            _client.SendMessage(e.ChatMessage.Channel, tgMessage);
        }
        else if (e.ChatMessage.Message.StartsWith("!yt"))
        {
            var ytMessage = "Разбор тем и прочие более душные штуки из мира программирования https://www.youtube.com/@Cleannetcode"
            _client.SendMessage(e.ChatMessage.Channel, ytMessage);
        }
    }
}