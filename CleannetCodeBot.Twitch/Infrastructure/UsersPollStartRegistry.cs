using System.Collections.Concurrent;
using CleannetCodeBot.Twitch.Polls;

namespace CleannetCodeBot.Twitch.Infrastructure;

public class UsersPollStartRegistry : IUsersPollStartRegistry
{
    private readonly ConcurrentDictionary<string, DateTime> _users = new();

    public bool AddUserTryRecord(string userId, DateTime nextTryDate)
    {
        return _users.TryAdd(userId, nextTryDate);
    }

    public void RemoveUserTryRecord(string userId)
    {
        _users.TryRemove(userId, out _);
    }

    public void ClearExpiredRecords(DateTime expiredOnDate)
    {
        foreach (var tryRecord in _users)
        {
            if (tryRecord.Value < expiredOnDate)
            {
                _users.TryRemove(tryRecord);
            }
        }
    }
}