using CleannetCodeBot.Features.Onboarding;

namespace CleannetCodeBot.Core;

public record Member
{
    public const string CollectionName = "members";
    
    public Member(long id,
        string? username,
        string firstName = "",
        string lastName = "",
        long? personalChatId = null,
        string? youtubeName = null,
        string? githubNick = null,
        bool started = false,
        WelcomeUserInfoState state = WelcomeUserInfoState.Idle)
    {
        Id = id;
        Username = username ?? $"{firstName}{(string.IsNullOrEmpty(lastName) ? string.Empty : " " + lastName)}";
        FirstName = firstName;
        LastName = lastName;
        PersonalChatId = personalChatId;
        YoutubeName = youtubeName;
        GithubNick = githubNick;
        Started = started;
        State = state;
    }

    public long Id { get; init; }
    public string Username { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public long? PersonalChatId { get; init; }
    public string? YoutubeName { get; init; }
    public string? GithubNick { get; init; }
    public bool Started { get; init; }
    public WelcomeUserInfoState State { get; init; }
}