using System.Collections.Concurrent;

namespace CleannetCodeBot.Twitch.Polls;

public class Poll
{
    // <UserId, Answer>
    private readonly ConcurrentDictionary<string, string> _votes;

    public string BroadcasterId { get; }
    
    public string RewardId { get; }
    
    public PollQuestion Question { get; }
    
    public string CreatedByUser { get; }

    public DateTime EndDate { get; }

    public Poll(PollQuestion question, string broadcasterId, string rewardId, string createdByUser, DateTime endDate)
    {
        Question = question;
        BroadcasterId = broadcasterId;
        RewardId = rewardId;
        CreatedByUser = createdByUser;
        EndDate = endDate;

        _votes = new ConcurrentDictionary<string, string>();
    }

    public void AddVote(string userId, string answer)
    {
        if (Question.IsAnswerExists(answer))
        {
            _votes.TryAdd(userId, answer);
        }
    }

    public List<Vote> Results()
    {
        // <UserId, Answer>
        return _votes.Select(x => 
                new Vote(Question.Id, x.Key, x.Value, Question.IsAnswerCorrect(x.Value)))
            .ToList();
    }
}