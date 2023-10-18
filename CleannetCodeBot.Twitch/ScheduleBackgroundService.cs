using CleannetCodeBot.Twitch.Polls;
using Microsoft.Extensions.Caching.Memory;

namespace CleannetCodeBot.Twitch;

public class ScheduleBackgroundService : BackgroundService
{
    private readonly IPollsRepository _pollsRepository;
    private readonly IUsersPollStartRegistry _usersPollStartRegistry;
    private readonly IPollsService _pollsService;
    private readonly IMemoryCache _memoryCache;

    public ScheduleBackgroundService(IPollsRepository pollsRepository, 
        IUsersPollStartRegistry usersPollStartRegistry,
        IPollsService pollsService, 
        IMemoryCache memoryCache)
    {
        _pollsRepository = pollsRepository;
        _usersPollStartRegistry = usersPollStartRegistry;
        _pollsService = pollsService;
        _memoryCache = memoryCache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1_000, stoppingToken);
            var authToken = _memoryCache.Get<AuthToken>(AuthToken.Key);

            if (authToken is not null)
            {
                var expiredPolls = _pollsRepository.GetExpiredPolls(DateTime.UtcNow);

                async void CloseExpiredPolls(Poll x) =>
                    await _pollsService.ClosePoll(x.RewardId, x.BroadcasterId, authToken.AccessToken);

                expiredPolls.ForEach(CloseExpiredPolls);
            }

            _usersPollStartRegistry.ClearExpiredRecords(DateTime.UtcNow);
        }
    }
}