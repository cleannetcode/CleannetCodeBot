// todo <> section in config

namespace CleannetCode_bot.Features.Homeworks;

public class HomeworksCache
{
    public Dictionary<string, DiscussionData>? DiscussionsData { get; set; }
}

public class DiscussionData
{
    public List<DiscussionMessages>? Messages { get; set; }
    public List<LinkData>? Links { get; set; }
}

public class LinkData
{
    public string? Link { get; set; }
    public string? LastCommit { get; set; }
    public DateTimeOffset? LastGithubUpdate { get; set; }
}