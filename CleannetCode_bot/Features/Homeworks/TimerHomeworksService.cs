using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;

namespace CleannetCode_bot.Features.Homeworks;

public sealed class TimerHomeworksService : IHostedService, IAsyncDisposable
{
    private Timer? _timer;
    private int _executionCount = 0;
    private HomeworksHandler _homeworksHandler;

    private readonly ITelegramBotClient _client;
    private readonly HomeworksServiceOptions _config;
    private readonly ILogger<TimerHomeworksService> _logger;

    public TimerHomeworksService(
        ITelegramBotClient client,
        IOptionsMonitor<HomeworksServiceOptions> homeworksServiceOptionsMonitor,
        ILogger<TimerHomeworksService> logger,
        ILogger<HomeworksHandler> loggerHomeworksHandler)
    {
        this._client = client;
        this._config = homeworksServiceOptionsMonitor.CurrentValue;
        this._logger = logger;

        _homeworksHandler = new HomeworksHandler(
            client,
            homeworksServiceOptionsMonitor.CurrentValue,
            loggerHomeworksHandler
        );
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Service} is running.", nameof(TimerHomeworksService));

        var delay = _config.CheckTimerInMinutes;

        _timer = new Timer(
            TimerDoWork,
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(delay));

        return Task.CompletedTask;
    }

    public void TimerDoWork(object? state)
    {
        int count = Interlocked.Increment(ref _executionCount);
        _logger.LogInformation("{Service} is working, execution count: {Count:#,0}", nameof(TimerHomeworksService), count);

        _homeworksHandler.Start();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Service} is stopping.", nameof(TimerHomeworksService));

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("{Service} is dispose.", nameof(TimerHomeworksService));

        if (_timer is IAsyncDisposable timer)
        {
            await _timer.DisposeAsync();
        }

        _timer = null;
    }
}

