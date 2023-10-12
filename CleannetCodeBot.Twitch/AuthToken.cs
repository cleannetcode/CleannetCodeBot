using System.Text.Json.Serialization;

namespace CleannetCodeBot.Twitch;

public record AuthToken
{
    public const string Key = "auth_token";

    public AuthToken(
        string AccessToken,
        int ExpiresIn,
        string refreshToken)
    {
        this.AccessToken = AccessToken;
        this.ExpiresIn = ExpiresIn;
        RefreshToken = refreshToken;
    }

    [JsonPropertyName("access_token")] public string AccessToken { get; init; }

    [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }

    [JsonPropertyName("refresh_token")] public string RefreshToken { get; init; }
}