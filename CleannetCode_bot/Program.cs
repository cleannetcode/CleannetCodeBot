using CleannetCode_bot.Features.Forwards;
using CleannetCode_bot.Features.Homework;
using CleannetCode_bot.Features.Welcome;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Telegram.Bot;

namespace CleannetCode_bot
{
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
                    services.Configure<ForwardsHandlerOptions>(context.Configuration.GetSection(ForwardsHandlerOptions.Section));
                    services.Configure<HomeworkServiceOptions>(context.Configuration.GetSection(HomeworkServiceOptions.Section));
                    services.AddScoped<IStorageService, StorageFileService>();
                    services.AddScoped<IForwardHandler, ForwardsHandler>();
                    services.AddScoped<Handlers>();
                    services.AddSingleton<WelcomeHandler>();
                    services.AddSingleton<HomeworkHandler>();
                    services.AddSingleton<ITelegramBotClient, TelegramBotClient>(_ =>
                    {
                        var accessToken = context.Configuration.GetValue<string>("AccessToken")!;
                        return new(accessToken);
                    });
                    services.AddHostedService<TimerHomeworkService>();
                    services.AddHostedService<BotService>();
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
}