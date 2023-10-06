using System.Text.Json.Serialization;

namespace CleannetCodeBot.Twitch;

public record AuthToken
{
    public const string Key = "auth_token";

    public AuthToken(string AccessToken,
        int ExpiresIn)
    {
        this.AccessToken = AccessToken;
        this.ExpiresIn = ExpiresIn;
    }

    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

}