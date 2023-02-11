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

        var discussionMessagesRepository = new DiscussionMessagesRepository(_config.FileNameCache ?? "cachedHomeworkMessages0.json");
        var newListMessage = new List<DiscussionMessages>();

        foreach (var organization in _config.Organizations ?? Array.Empty<Organization>())
        {
            foreach (var repository in organization.Repositories ?? Array.Empty<Repository>())
            {
                foreach (var discussionID in repository.DiscussionsID ?? Array.Empty<int>())
                {
                    var allListMessages = await discussionMessagesRepository.GetMessagesFromDiscussion(
                        organization.OrganizationName ?? "",
                        repository.RepositoryName ?? "",
                        discussionID);

                    newListMessage.AddRange(discussionMessagesRepository.UpdateCacheAndGetNewMessages(
                        organization.OrganizationName ?? "",
                        repository.RepositoryName ?? "",
                        discussionID,
                        allListMessages));
                }
            }
        }

        foreach (var messagePage in newListMessage)
        {
            await _client.SendTextMessageAsync(
                _config.TelegramChannelID ?? "",
                $"Author: {messagePage.Author}\n\n {messagePage.Message}"); // messagePage.DatetimeCreateNode
        }

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

