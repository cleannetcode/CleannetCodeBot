namespace CleannetCodeBot.Twitch.Polls;

public interface IPollsRepository
{
    public Poll? GetPollByRewardId(string pollRewardId);

    public void RemovePollByRewardId(string pollRewardId);

    public void AddPoll(Poll poll);

    public List<Poll> GetExpiredPolls(DateTime expiredOnDate);

}