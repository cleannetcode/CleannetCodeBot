using CleannetCode_bot.Features.Forwards;
using CleannetCode_bot.Features.Statistics;
using CleannetCode_bot.Features.Welcome;
using CleannetCode_bot.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CleannetCode_bot;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, configuration) =>
            {
                configuration.Sources.Clear();

                var env = hostingContext.HostingEnvironment;

                configuration
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddHandlerChains(typeof(Program).Assembly);

                services.AddHostedService<BotBackgroundService>();

                services.Configure<ForwardsHandlerOptions>(context.Configuration.GetSection(ForwardsHandlerOptions.Section));

                services.AddSingleton<WelcomeHandler>();
                services.AddSingleton<ITelegramBotClient, TelegramBotClient>(_ =>
                {
                    var accessToken = context.Configuration.GetValue<string>("AccessToken")!;
                    return new(accessToken);
                });
                
                services.AddScoped(LogHandlerChain<CallbackQuery>.Factory("callbackQuery", x => x.Update.CallbackQuery));
                services.AddScoped(LogHandlerChain<Message>.Factory("channelPost", x => x.Update.ChannelPost));
                services.AddScoped(LogHandlerChain<ChatJoinRequest>.Factory("chatJoinRequest", x => x.Update.ChatJoinRequest));
                services.AddScoped(LogHandlerChain<ChatMemberUpdated>.Factory("chatMember", x => x.Update.ChatMember));
                services.AddScoped(LogHandlerChain<ChosenInlineResult>.Factory("chosenInlineResult", x => x.Update.ChosenInlineResult));
                services.AddScoped(LogHandlerChain<Message>.Factory("editedChannelPost", x => x.Update.EditedChannelPost));
                services.AddScoped(LogHandlerChain<Message>.Factory("editedMessage", x => x.Update.EditedMessage));
                services.AddScoped(LogHandlerChain<InlineQuery>.Factory("inlineQuery", x => x.Update.InlineQuery));
                services.AddScoped(LogHandlerChain<Message>.Factory("message", x => x.Update.Message));
                services.AddScoped(LogHandlerChain<ChatMemberUpdated>.Factory("myChatMember", x => x.Update.MyChatMember));
                services.AddScoped(LogHandlerChain<PollAnswer>.Factory("pollAnswer", x => x.Update.PollAnswer));

                services.AddScoped<IGenericStorageService, StorageFileService>();
                services.AddScoped<IForwardHandler, ForwardsHandler>();
                services.AddScoped<Handlers>();
            })
            .ConfigureLogging((context, logging) =>
                logging.ClearProviders()
                    .AddSerilog(new LoggerConfiguration()
                        .ReadFrom.Configuration(context.Configuration)
                        .CreateLogger()))
            .Build();

        await host.StartAsync();
    }
}