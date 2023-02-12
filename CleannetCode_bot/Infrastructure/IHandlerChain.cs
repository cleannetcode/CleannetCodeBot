using CSharpFunctionalExtensions;

namespace CleannetCode_bot.Infrastructure;

public interface IHandlerChain
{
    int OrderInChain { get; }

    Task<Result> HandleAsync(TelegramRequest request, CancellationToken cancellationToken = default);
}