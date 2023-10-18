using System.Collections.Concurrent;
using CleannetCodeBot.Twitch.Polls;

namespace CleannetCodeBot.Twitch.Infrastructure;

public class PollsRepository : IPollsRepository
{
    private readonly ConcurrentDictionary<string, Poll> _pollsRegistry;

    public PollsRepository()
    {
        _pollsRegistry = new ConcurrentDictionary<string, Poll>();
    }
    
    public Poll? GetPollByRewardId(string pollRewardId)
    {
        _pollsRegistry.TryGetValue(pollRewardId, out var poll);
        return poll;
    }

    public void RemovePollByRewardId(string pollRewardId)
    {
        _pollsRegistry.TryRemove(pollRewardId, out _);
    }

    public void AddPoll(Poll poll)
    {
        _pollsRegistry.TryAdd(poll.RewardId, poll);
    }

    public List<Poll> GetExpiredPolls(DateTime expiredOnDate)
    {
        return _pollsRegistry.Where(x => x.Value.EndDate < expiredOnDate)
            .Select(x => x.Value)
            .ToList();
    }
}