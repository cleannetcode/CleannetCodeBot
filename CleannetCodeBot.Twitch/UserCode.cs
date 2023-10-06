namespace CleannetCodeBot.Twitch;

public record UserCode(string Code, string Scope, string State)
{
    public const string Key = "user_code";
}