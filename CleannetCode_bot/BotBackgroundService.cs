using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CleannetCode_bot;

public class BotBackgroundService : IHostedService
{
    private readonly ILogger<BotBackgroundService> logger;
    private readonly ITelegramBotClient client;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ConcurrentBag<Task> awaitedTasks = new();
    private readonly CancellationTokenSource cancellationTokenSource = new();

    public BotBackgroundService(
        ILogger<BotBackgroundService> logger,
        ITelegramBotClient client,
        IServiceScopeFactory serviceScopeFactory)
    {
        this.logger = logger;
        this.client = client;
        this.serviceScopeFactory = serviceScopeFactory;
    }

    public async Task RunAsync()
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

        var me = await client.GetMeAsync(cancellationTokenSource.Token);

        logger.LogInformation("{DateTime:dd.MM.yyyy HH:mm:ss:ffff}\tHey! I am {BotName}", DateTime.Now,
            me.Username);

        await client.ReceiveAsync(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cancellationTokenSource.Token
        );
    }

    private Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cts)
    {
        // Типы обновлений смотри тут: https://core.telegram.org/bots/api#Update : IMessageUpdate
        //--------------------------------------------------
        // Разобратся с типом getUpdates. Попытаться запросить историю уже прочитанных сообщений: https://core.telegram.org/bots/api#getupdates
        //--------------------------------------------------

        awaitedTasks.Add(ServeUpdate(update, cts));
        return Task.CompletedTask;
    }

    private async Task ServeUpdate(Update update, CancellationToken cts)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var updateLogger = scope.ServiceProvider.GetRequiredService<ILogger<BotBackgroundService>>();
        var handlers = scope.ServiceProvider.GetRequiredService<Infrastructure.Handlers>();
        var stopwatch = Stopwatch.StartNew();
        updateLogger.LogDebug("Start serving update of type {Type} with id {Id}", update.Type, update.Id);
        try
        {
            var result = await handlers.ExecuteAsync(update, cts);
            if (result.IsFailure)
            {
                updateLogger.LogError(result.Error);
            }
        }
        catch (Exception exception)
        {
            updateLogger.LogError(exception, "Error occurred during update serving");
        }

        stopwatch.Stop();
        updateLogger.LogDebug("Finish serving update of type {Type} with id {Id} elapsed {Elapsed} ms", update.Type,
            update.Id, stopwatch.ElapsedMilliseconds);
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        logger.LogError(exception, errorMessage);
        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RunAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationTokenSource.Cancel();
        await Task.WhenAll(awaitedTasks.Select(task => task.WaitAsync(cancellationToken)));
    }
}