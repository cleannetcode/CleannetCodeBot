namespace CleannetCode_bot.Features.Forwards;

public sealed class ForwardsHandlerOptions
{
    public const string Section = "ForwardsHandlerOptions";
    public Dictionary<long, Dictionary<int, HashSet<long>>> ChatsWithRestrictedTopicsWithAllowedUsersToWrite { get; set; } = new();

    public Dictionary<long, Dictionary<int, ForwardingMap>> ChatsWithTopicsForwardMapping { get; set; } = new();

    public record ForwardingMap(long ChatId, int? ThreadId);
}