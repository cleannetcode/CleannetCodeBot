using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Microsoft.Extensions.Options;

namespace CleannetCode_bot.Features.Homeworks;

public sealed class TimerHomeworksService : IHostedService, IAsyncDisposable
{
    private Timer? _timer;
    private int _executionCount = 0;

    private readonly ITelegramBotClient _client;
    private readonly IOptionsMonitor<HomeworksServiceOptions> _homeworksServiceOptionsMonitor;
    private readonly ILogger<TimerHomeworksService> _logger;
    private HomeworksServiceOptions _config => _homeworksServiceOptionsMonitor.CurrentValue;

    public TimerHomeworksService(
        ITelegramBotClient client,
        IOptionsMonitor<HomeworksServiceOptions> homeworksServiceOptionsMonitor,
        ILogger<TimerHomeworksService> logger)
    {
        this._client = client;
        this._homeworksServiceOptionsMonitor = homeworksServiceOptionsMonitor;
        this._logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Service} is running.", nameof(TimerHomeworksService));

        var delay = _config.CheckTimerInMinutes;

        _timer = new Timer(
            DoWork,
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(delay));

        return Task.CompletedTask;
    }

    public async void DoWork(object? state)
    {
        int count = Interlocked.Increment(ref _executionCount);
        _logger.LogInformation("{Service} is working, execution count: {Count:#,0}", nameof(TimerHomeworksService), count);

        var organizationName = _config.Organizations?[0].OrganizationName;
        var repositoryName = _config.Organizations?[0].Repositories?[0].RepositoryName;
        var discussionID = _config.Organizations?[0].Repositories?[0].DiscussionsID?[0] ?? 32;

        var allListMessages = await DiscussionMessagesRepository.GetMessagesFromDiscussion(organizationName, repositoryName, discussionID);
        var newListMessage = DiscussionMessagesRepository.UpdateCacheAndGetNewMessages(organizationName, repositoryName, discussionID, allListMessages);
        // SendMessages(newListMessage);
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

