using CleannetCodeBot.Twitch.Polls;
using Microsoft.Extensions.Caching.Memory;

namespace CleannetCodeBot.Twitch;

public class ScheduleBackgroundService : BackgroundService
{
    private readonly IPollsRepository _pollsRepository;
    private readonly IUsersPollStartRegistry _usersPollStartRegistry;
    private readonly IPollsService _pollsService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ScheduleBackgroundService> _logger;
    
    private readonly TimeSpan _timerResolution = TimeSpan.FromMinutes(1); 

    public ScheduleBackgroundService(IPollsRepository pollsRepository, 
        IUsersPollStartRegistry usersPollStartRegistry,
        IPollsService pollsService, 
        IMemoryCache memoryCache,
        ILogger<ScheduleBackgroundService> logger)
    {
        _pollsRepository = pollsRepository;
        _usersPollStartRegistry = usersPollStartRegistry;
        _pollsService = pollsService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service running.");
        
        using PeriodicTimer timer = new(_timerResolution);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await DoWork();
        }

        _logger.LogInformation("Timed Hosted Service is stopping.");
    }

    private async Task DoWork()
    {
        var authToken = _memoryCache.Get<AuthToken>(AuthToken.Key);

        if (authToken is not null)
        {
            _logger.LogInformation("Searching for expired polls");
            var expiredPolls = _pollsRepository.GetExpiredPolls(DateTime.UtcNow);
            _logger.LogInformation($"Found {expiredPolls.Count} expired polls");

            foreach (var expiredPoll in expiredPolls)
            {
                await _pollsService.ClosePoll(expiredPoll.RewardId, expiredPoll.BroadcasterId, authToken.AccessToken);
            }
        }

        _logger.LogInformation($"Clear expired user records at {DateTime.UtcNow}");
        _usersPollStartRegistry.ClearExpiredRecords(DateTime.UtcNow);
    }
}