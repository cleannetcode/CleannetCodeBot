using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types.Enums;

namespace CleannetCode_bot.Tests
{
    public class ServiceCollectionUpdateHandlersExtensionsTests
    {
        [Fact]
        public void AddUpdateHandlers_ShouldRegisterHandlersMap()
        {
            // arrange
            var serviceCollection= new ServiceCollection();

            // act
            ServiceCollectionUpdateHandlersExtensions.AddUpdateHandlers(serviceCollection);
    
            // assert
            var handlersMap = serviceCollection
                .Where(x => x.ServiceType == typeof(HandlersMap))
                .ToArray();

            Assert.NotEmpty(handlersMap);
        }

        [Fact]
        public void AddUpdateHandlers_TestHandlerRegistered_ShouldReturnTestHandler()
        {
            // arrange
            var serviceCollection= new ServiceCollection();
            var testAssembly = GetType().Assembly;

            // act
            ServiceCollectionUpdateHandlersExtensions.AddUpdateHandlers(serviceCollection, testAssembly);
    
            // assert
            var updateHandlers = serviceCollection
                .Where(x => x.ServiceType == typeof(TestUpdateHandler))
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
            ServiceCollectionUpdateHandlersExtensions.AddUpdateHandlers(serviceCollection, assemblyWithoutUpdateHandlers);
    
            // assert
            var updateHandlers = serviceCollection
                .Where(x => x.ServiceType == typeof(TestUpdateHandler))
                .ToArray();

            Assert.Empty(updateHandlers);
        }

        [Fact]
        public void AddUpdateHandlers_UpdateHandlersAreNotRegistered_ShouldNotReturnAnyHandlers()
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            // act
            ServiceCollectionUpdateHandlersExtensions.AddUpdateHandlers(serviceCollection);
    
            // assert
            var updateHandlers = serviceCollection
                .Where(x => x.ServiceType
                    .GetInterfaces()
                    .Any(i => i == typeof(IUpdateHandler)))
                .ToArray();

            Assert.Empty(updateHandlers);
        }
        
        [Fact]
        public void GetHandlers_ShouldReturnTestUpdateHandler()
        {
            // arrange
            var serviceCollection= new ServiceCollection();
            var testAssembly = GetType().Assembly;
            ServiceCollectionUpdateHandlersExtensions
                .AddUpdateHandlers(serviceCollection, testAssembly);
            var provider = serviceCollection.BuildServiceProvider();
            var handlersMap = provider.GetRequiredService<HandlersMap>();

            // act
            var handlers = handlersMap.GetHandlers(UpdateType.Message);
    
            // assert
            Assert.NotEmpty(handlers);
        }

        [SupportedUpdateType(UpdateType.Message)]
        private sealed class TestUpdateHandler : IUpdateHandler
        {
            public Task<CSharpFunctionalExtensions.Result> HandleAsync()
            {
                throw new NotImplementedException();
            }
        }
    }
}