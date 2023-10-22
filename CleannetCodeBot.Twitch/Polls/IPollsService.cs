namespace CleannetCodeBot.Twitch.Polls;

public interface IPollsService
{
    public Task CreatePoll(string userId, string username, string broadCasterId, string authToken);

    public Task ClosePoll(string pollRewardId, string broadcasterId, string authToken);

    public void AddVoteToPoll(string pollRewardId, string userId, string answer);
}