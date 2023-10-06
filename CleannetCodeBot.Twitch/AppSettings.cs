namespace CleannetCodeBot.Twitch;

public record AppSettings
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string RedirectUri { get; init; }
}