using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CleannetCodeBot.Infrastructure;

public static class ServiceCollectionUpdateHandlersExtensions
{
    public static IServiceCollection AddHandlerChains(
        this IServiceCollection serviceCollection,
        params Assembly[] selectedAssemblies)
    {
        serviceCollection.AddScoped<Handlers>();

        var handlers = selectedAssemblies
            .SelectMany(x => x.GetTypes())
            .Where(type => type is { IsInterface: false, IsAbstract: false } 
                           && type.GetInterfaces().Any(i => i == typeof(IHandlerChain)) 
                           && type.GetCustomAttributes().OfType<IgnoreAutoInjectionAttribute>().Any() == false)
            .ToHashSet();

        foreach (var handler in handlers)
        {
            serviceCollection.AddSingleton(
                serviceType: typeof(IHandlerChain),
                implementationType: handler);
        }

        return serviceCollection;
    }
}