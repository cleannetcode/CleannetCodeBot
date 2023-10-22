namespace CleannetCodeBot.Twitch.Polls;

public record PollSettings
{
    public required string RewardTitle { get; init; }
    
    public required int PollDurationInMinutes { get; init; }
    
    public required int UserPollCreationGapInHours { get; init; }
}