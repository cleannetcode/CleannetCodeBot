using CleannetCode_bot.Infrastructure;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;

namespace CleannetCode_bot.Tests;

public class ServiceCollectionUpdateHandlersExtensionsTests
{
    [Fact]
    public void AddUpdateHandlers_ShouldRegisterHandlersMap()
    {
        // arrange
        var serviceCollection= new ServiceCollection();

        // act
        serviceCollection.AddHandlerChains();
    
        // assert
        var handlersMap = serviceCollection
            .Where(x => x.ServiceType == typeof(Infrastructure.Handlers))
            .ToArray();

        Assert.NotEmpty(handlersMap);
    }

    [Fact]
    public void AddUpdateHandlers_IgnoredTestHandlerChainRegistered_ShouldNotReturnIgnoredTestHandlerChain()
    {
        // arrange
        var serviceCollection= new ServiceCollection();
        var testAssembly = GetType().Assembly;

        // act
        serviceCollection.AddHandlerChains(testAssembly);
    
        // assert
        Assert.DoesNotContain(serviceCollection, x => x.ServiceType == typeof(IgnoredTestUpdateHandlerChain));
    }


    [Fact]
    public void AddUpdateHandlers_TestHandlerChainRegistered_ShouldReturnTestHandlerChain()
    {
        // arrange
        var serviceCollection= new ServiceCollection();
        var testAssembly = GetType().Assembly;

        // act
        serviceCollection.AddHandlerChains(testAssembly);
    
        // assert
        var updateHandlers = serviceCollection
            .Where(x => x.ServiceType == typeof(TestUpdateHandlerChain))
            .ToArray();

        Assert.NotEmpty(updateHandlers);
    }

    [Fact]
    public void AddUpdateHandlers_TestHandlerIsNotRegistered_ShouldNotReturnTestHandler()
    {
        // arrange
        var serviceCollection = new ServiceCollection();
        var assemblyWithoutUpdateHandlers = typeof(string).Assembly;

        // act
        serviceCollection.AddHandlerChains(assemblyWithoutUpdateHandlers);
    
        // assert
        var updateHandlers = serviceCollection
            .Where(x => x.ServiceType == typeof(TestUpdateHandlerChain))
            .ToArray();

        Assert.Empty(updateHandlers);
    }

    [Fact]
    public void AddUpdateHandlers_UpdateHandlersAreNotRegistered_ShouldNotReturnAnyHandlers()
    {
        // arrange
        var serviceCollection = new ServiceCollection();

        // act
        serviceCollection.AddHandlerChains();
    
        // assert
        var updateHandlers = serviceCollection
            .Where(x => x.ServiceType
                .GetInterfaces()
                .Any(i => i == typeof(IHandlerChain)))
            .ToArray();

        Assert.Empty(updateHandlers);
    }
        
    [Fact]
    public async Task GetHandlers_ShouldReturnTestUpdateHandler()
    {
        // arrange
        var serviceCollection= new ServiceCollection();
        var testAssembly = GetType().Assembly;
        serviceCollection.AddHandlerChains(testAssembly);
        var provider = serviceCollection.BuildServiceProvider();
        var handlers = provider.GetRequiredService<Infrastructure.Handlers>();
        var update = new Update();

        // act
        var result = await handlers.ExecuteAsync(update, CancellationToken.None);
    
        // assert
        Assert.True(result.IsSuccess);
    }

    private sealed class TestUpdateHandlerChain : IHandlerChain
    {
        public int OrderInChain { get; }
        public Task<Result> HandleAsync(TelegramRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }
    }
    
    [IgnoreAutoInjection]
    private sealed class IgnoredTestUpdateHandlerChain : IHandlerChain
    {
        public int OrderInChain { get; }
        public Task<Result> HandleAsync(TelegramRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }
    }
}