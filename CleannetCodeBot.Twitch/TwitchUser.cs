namespace CleannetCodeBot.Twitch;

public class TwitchUser
{
    public static readonly string CollectionName = "users";
    
    public string TwitchUserId { get; init; }
    
    public string Nickname { get; init; }
}