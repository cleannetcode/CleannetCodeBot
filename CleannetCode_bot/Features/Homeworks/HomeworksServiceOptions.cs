
namespace CleannetCode_bot.Features.Homeworks;

public sealed class HomeworksServiceOptions
{
    public const string Section = "HomeworksServiceOptions";

    public int CheckTimerInMinutes { get; set; }

    public string? FileNameCache { get; set; }

    public string? TelegramChannelID { get; set; }

    public Organization[]? Organizations { get; set; }
}

public class Organization
{
    public string? OrganizationName { get; set; }
    public Repository[]? Repositories { get; set; }
}

public class Repository
{
    public string? RepositoryName { get; set; }
    public int[]? DiscussionsID { get; set; }
}