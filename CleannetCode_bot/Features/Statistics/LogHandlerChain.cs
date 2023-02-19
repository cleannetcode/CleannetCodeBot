using CleannetCode_bot.Infrastructure;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.DependencyInjection;

namespace CleannetCode_bot.Features.Statistics;

[IgnoreAutoInjection]
public class LogHandlerChain<TMessage> : IHandlerChain
{
    private readonly Func<TelegramRequest, TMessage?> _resolver;
    private readonly IGenericStorageService _genericStorageService;
    private readonly string _messageName;
        
    private LogHandlerChain(
        int orderInChain, 
        string messageName, 
        Func<TelegramRequest, TMessage?> resolver,
        IGenericStorageService genericStorageService)
    {
        OrderInChain = orderInChain;
        _resolver = resolver;
        _genericStorageService = genericStorageService;
        _messageName = messageName;
    }

    public int OrderInChain { get; }

    public async Task<Result> HandleAsync(TelegramRequest request, CancellationToken cancellationToken = default)
    {
        var result = _resolver(request);
        if (result is null) return HandlerResults.NotMatchingType;
        await _genericStorageService.AddObjectAsync(result, _messageName, cancellationToken);
        return Result.Success($"Successful handling of {_messageName}");
    }
        
    public static Func<IServiceProvider, IHandlerChain> Factory(string messageName, Func<TelegramRequest, TMessage?> resolver)
    {
        return serviceProvider => new LogHandlerChain<TMessage>(0,
            messageName,
            resolver,
            serviceProvider.GetRequiredService<IGenericStorageService>());
    }
}