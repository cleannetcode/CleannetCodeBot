using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types.Enums;

namespace CleannetCode_bot
{
    public static class ServiceCollectionUpdateHandlersExtensions
    {
        public static IServiceCollection AddUpdateHandlers(
            this IServiceCollection serviceCollection,
            params Assembly[] selectedAssemblies)
        {
            var handlers = selectedAssemblies
                .SelectMany(x => x.GetTypes())
                .Where(type => type.IsInterface == false
                    && type.GetInterfaces().Any(i => i == typeof(IUpdateHandler)))
                .ToHashSet();

            var updateTypesHandlers = new Dictionary<UpdateType, HashSet<Type>>();
            foreach (var handler in handlers)
            {
                serviceCollection.AddSingleton(handler);

                var updateTypes = handler.GetCustomAttributes<SupportedUpdateType>()
                    .Select(x => x.UpdateType)
                    .ToHashSet();

                foreach (var updateType in updateTypes)
                {
                    if (updateTypesHandlers.TryGetValue(updateType, out var handlerTypes))
                    {
                        handlerTypes.Add(handler);
                    }
                    else
                    {
                        updateTypesHandlers.Add(updateType, new HashSet<Type>() { handler });
                    }
                }
            }


            serviceCollection.AddSingleton<HandlersMap>(provider =>
            {
                var map = new Dictionary<UpdateType, Func<IUpdateHandler[]>>();

                foreach (var updateTypeHandlers in updateTypesHandlers)
                {
                    map.Add(updateTypeHandlers.Key, () =>
                    {
                        return updateTypeHandlers.Value
                            .Select(x => provider.GetService(x))
                            .OfType<IUpdateHandler>()
                            .ToArray();
                    });
                }
                var handlerMap = new HandlersMap(map);
                return handlerMap;
            });
            return serviceCollection;
        }
    }
}