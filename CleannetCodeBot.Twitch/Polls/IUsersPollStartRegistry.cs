namespace CleannetCodeBot.Twitch.Polls;

public interface IUsersPollStartRegistry
{
    public bool AddUserTryRecord(string userId, DateTime nextTryDate);
    public void RemoveUserTryRecord(string userId);
    public void ClearExpiredRecords(DateTime expiredOnDate);
}