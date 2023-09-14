using CleannetCodeBot.Core;
using CleannetCodeBot.Features.Forwards;
using CleannetCodeBot.Features.Statistics;
using CleannetCodeBot.Features.Welcome;
using CleannetCodeBot.Infrastructure;
using CleannetCodeBot.Infrastructure.DataAccess;
using CleannetCodeBot.Infrastructure.DataAccess.Interfaces;
using CleannetCodeBot.Infrastructure.Multithreading;
using CleannetCodeBot.Infrastructure.Multithreading.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CleannetCodeBot;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var environmentVariable = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
        var host = Host.CreateDefaultBuilder(args)
            .UseEnvironment(environmentVariable)
            .ConfigureServices(
                (context, services) =>
                {
                    services.AddHandlerChains(typeof(Program).Assembly);

                    services.AddHostedService<BotBackgroundService>();
                    services.AddSingleton<ITelegramBotClient, TelegramBotClient>(
                        _ =>
                        {
                            var accessToken = context.Configuration.GetValue<string>("AccessToken")!;
                            return new(accessToken);
                        });

                    services.AddSingleton<IMongoDatabase>(_ =>
                    {
                        var connectionString = context.Configuration.GetValue<string>("MongoDbConnectionString")!;
                        var client = new MongoClient(connectionString);
                        return client.GetDatabase("CleanCodeBotDb");
                    });

                    services.AddSingleton(serviceType: typeof(ILockService<,>), implementationType: typeof(SemaphoreSlimLockService<,>));
                    services.AddSingleton(serviceType: typeof(IGenericRepository<,>), implementationType: typeof(JsonFilesGenericRepository<,>));
                    services.Configure<JsonFilesGenericRepositoryOptions<long, Member>>(
                        context.Configuration.GetSection(JsonFilesGenericRepositoryOptions<long, Member>.GetSectionName()));
                    services.Configure<ForwardsHandlerOptions>(context.Configuration.GetSection(ForwardsHandlerOptions.Section));
                    services.Configure<WelcomeBotClientOptions>(context.Configuration.GetSection(WelcomeBotClientOptions.Section));

                    services.AddSingleton<IStickersBotClient, StickersBotClient>();
                    services.AddSingleton<IWelcomeBotClient, WelcomeBotClient>();

                    services.AddScoped(
                        LogHandlerChain<CallbackQuery>.Factory(
                            messageName: "callbackQuery",
                            resolver: x => x.Update.CallbackQuery));
                    services.AddScoped(
                        LogHandlerChain<Message>.Factory(
                            messageName: "channelPost",
                            resolver: x => x.Update.ChannelPost));
                    services.AddScoped(
                        LogHandlerChain<ChatJoinRequest>.Factory(
                            messageName: "chatJoinRequest",
                            resolver: x => x.Update.ChatJoinRequest));
                    services.AddScoped(
                        LogHandlerChain<ChatMemberUpdated>.Factory(
                            messageName: "chatMember",
                            resolver: x => x.Update.ChatMember));
                    services.AddScoped(
                        LogHandlerChain<ChosenInlineResult>.Factory(
                            messageName: "chosenInlineResult",
                            resolver: x => x.Update.ChosenInlineResult));
                    services.AddScoped(
                        LogHandlerChain<Message>.Factory(
                            messageName: "editedChannelPost",
                            resolver: x => x.Update.EditedChannelPost));
                    services.AddScoped(
                        LogHandlerChain<Message>.Factory(
                            messageName: "editedMessage",
                            resolver: x => x.Update.EditedMessage));
                    services.AddScoped(
                        LogHandlerChain<InlineQuery>.Factory(
                            messageName: "inlineQuery",
                            resolver: x => x.Update.InlineQuery));
                    services.AddScoped(
                        LogHandlerChain<Message>.Factory(
                            messageName: "message",
                            resolver: x => x.Update.Message));
                    services.AddScoped(
                        LogHandlerChain<ChatMemberUpdated>.Factory(
                            messageName: "myChatMember",
                            resolver: x => x.Update.MyChatMember));
                    services.AddScoped(
                        LogHandlerChain<PollAnswer>.Factory(
                            messageName: "pollAnswer",
                            resolver: x => x.Update.PollAnswer));

                    services.AddScoped<IGenericStorageService, StorageFileService>();
                    // services.AddScoped<IForwardHandler, ForwardsHandler>();
                    services.AddScoped<Handlers>();
                })
            .ConfigureLogging(
                (context, logging) =>
                    logging.ClearProviders()
                        .AddSerilog(
                            new LoggerConfiguration()
                                .ReadFrom.Configuration(context.Configuration)
                                .CreateLogger()))
            .Build();

        await host.StartAsync();
    }
}