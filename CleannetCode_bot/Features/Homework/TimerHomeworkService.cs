using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using CleannetCode_bot.Features.Homework.Models;

namespace CleannetCode_bot.Features.Homework;

public class TimerHomeworkService : BackgroundService
{
    private readonly HomeworkServiceOptions _config;
    private readonly ILogger<TimerHomeworkService> _logger;
    private readonly HomeworkHandler _homeworkHandler;

    public TimerHomeworkService(
        ILogger<TimerHomeworkService> logger,
        IOptionsMonitor<HomeworkServiceOptions> homeworksServiceOptionsMonitor,
        HomeworkHandler homeworkHandler)
    {
        _config = homeworksServiceOptionsMonitor.CurrentValue;
        _logger = logger;
        _homeworkHandler = homeworkHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromMinutes(_config.CheckTimerInMinutes);

        await Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _homeworkHandler.Start();
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }, cancellationToken);
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        base.StartAsync(cancellationToken);

        _logger.LogInformation("{Service} is running.", nameof(TimerHomeworkService));

        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        base.StopAsync(cancellationToken);

        _logger.LogInformation("{Service} is stopping.", nameof(TimerHomeworkService));

        return Task.CompletedTask;
    }
}