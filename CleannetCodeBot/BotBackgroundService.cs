using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CleannetCodeBot;

public class BotBackgroundService : IHostedService
{
    private readonly ILogger<BotBackgroundService> _logger;
    private readonly ITelegramBotClient _client;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public BotBackgroundService(
        ILogger<BotBackgroundService> logger,
        ITelegramBotClient client,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _client = client;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return RunAsync(cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        // Начать получение не блокирует поток вызывающего абонента. Получение выполняется в пуле потоков.
        var receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
                UpdateType.InlineQuery,
                UpdateType.ChosenInlineResult,
                UpdateType.CallbackQuery,
                UpdateType.EditedMessage,
                UpdateType.ChannelPost,
                UpdateType.EditedChannelPost,
                UpdateType.ShippingQuery,
                UpdateType.PreCheckoutQuery,
                UpdateType.Poll,
                UpdateType.PollAnswer,
                UpdateType.MyChatMember,
                UpdateType.ChatMember,
                UpdateType.ChatJoinRequest
            }
        };

        var me = await _client.GetMeAsync(cancellationToken: cancellationToken);
        _logger.LogInformation(
            "{DateTime:dd.MM.yyyy HH:mm:ss:ffff}\tHey! I am {BotName}", DateTime.Now,
            me.Username);

        await _client.ReceiveAsync(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cancellationToken);
    }

    private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cts)
    {
        // Типы обновлений смотри тут: https://core.telegram.org/bots/api#Update : IMessageUpdate
        //--------------------------------------------------
        // Разобратся с типом getUpdates. Попытаться запросить историю уже прочитанных сообщений: https://core.telegram.org/bots/api#getupdates
        //--------------------------------------------------
        return ServeUpdate(update, cts);
    }

    private async Task ServeUpdate(Update update, CancellationToken cts)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var updateLogger = scope.ServiceProvider.GetRequiredService<ILogger<BotBackgroundService>>();
        var handlers = scope.ServiceProvider.GetRequiredService<Infrastructure.Handlers>();
        var stopwatch = Stopwatch.StartNew();
        updateLogger.LogDebug("Start serving update of type {Type} with id {Id}", update.Type, update.Id);
        try
        {
            var result = await handlers.ExecuteAsync(update, cts);
            if (result.IsFailure)
            {
                updateLogger.LogError("Result Failed: {Error}", result.Error);
            }
        }
        catch (Exception exception)
        {
            updateLogger.LogError(exception, "Error occurred during update serving");
        }

        stopwatch.Stop();
        updateLogger.LogDebug(
            message: "Finish serving update of type {Type} with id {Id} elapsed {Elapsed} ms",
            update.Type,
            update.Id,
            stopwatch.ElapsedMilliseconds);
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Telegram API Error {ErrorMessage}", exception.Message);
        return Task.CompletedTask;
    }
}