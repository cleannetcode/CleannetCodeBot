using CleannetCode_bot.Infrastructure;
using CSharpFunctionalExtensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CleannetCode_bot.Features.Statistics;

public sealed class CallbackQueryHandlerChain : IHandlerChain
{
    private readonly IStorageService _storage;

    public CallbackQueryHandlerChain(IStorageService storage)
    {
        _storage = storage;
    }

    public int OrderInChain => -9999;

    public async Task<Result> HandleAsync(TelegramRequest request, CancellationToken cancellationToken = default)
    {
        return await Result.SuccessIf(request.Update.Type == UpdateType.CallbackQuery, "Not match type")
            .Bind(() => Result.Success(request.Update.CallbackQuery))
            .Bind(x => Result.FailureIf(x == null, x, null))
            .Tap(x => _storage.AddObject(x, typeof(CallbackQuery), "callbackQuery", cancellationToken));
    }
}