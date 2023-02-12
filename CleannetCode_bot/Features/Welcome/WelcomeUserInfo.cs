namespace CleannetCode_bot.Features.Welcome;

public record WelcomeUserInfo(
    long Id,
    string Username,
    string FirstName = "",
    string LastName = "",
    int? YoutubeMessageId = null,
    string? YoutubeNick = null,
    int? GithubMessageId = null,
    string? GithubNick = null,
    int? PositionMessageId = null,
    int? PositionId = null,
    Position? Position = null,
    State State = State.None);