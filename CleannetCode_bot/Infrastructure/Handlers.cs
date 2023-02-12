using CSharpFunctionalExtensions;
using Telegram.Bot.Types;

namespace CleannetCode_bot.Infrastructure;

public class Handlers
{
    private readonly IReadOnlyCollection<IHandlerChain> _handlerChain;
    
    public Handlers(IEnumerable<IHandlerChain> handlerChain)
    {
        _handlerChain = handlerChain.OrderBy(x => x.OrderInChain).ToList();
    }

    public async Task<Result> ExecuteAsync(Update update, CancellationToken cancellationToken)
    {
        var request = new TelegramRequest(update);
        var results = new List<Result>(_handlerChain.Count);

        foreach (var handler in _handlerChain)
        {
            var result = await handler.HandleAsync(request, cancellationToken);
            results.Add(result);
        }
        return Result.Combine(results);
    }
}