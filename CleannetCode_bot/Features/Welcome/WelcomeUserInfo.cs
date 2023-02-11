namespace CleannetCode_bot.Features.Welcome;

public record WelcomeUserInfo(
    long Id,
    string Username,
    string FirstName = "",
    string LastName = "",
    long? PersonalChatId = null,
    string? YoutubeName = null,
    string? GithubNick = null,
    bool Started = false,
    WelcomeUserInfoState State = WelcomeUserInfoState.Idle);