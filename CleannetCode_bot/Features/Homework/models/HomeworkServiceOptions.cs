namespace CleannetCode_bot.Features.Homework;

public class HomeworkServiceOptions
{
    public const string Section = "HomeworkServiceOptions";

    public string GithubToken { get; init; } = string.Empty;
    public int CheckTimerInMinutes { get; init; }
    public string TgChannelID { get; init; } = string.Empty;
    public int TgThreadIdForNewComments { get; init; }
    public int TgThreadIdForNewRepoUpdates { get; init; }
    public string FileNameStorageCommentsDiscussions { get; init; } = string.Empty;
    public string FileNameStorageHomeworkOnRepositories { get; init; } = string.Empty;
    public string Organization { get; init; } = string.Empty;
    public RepositoryMetaData[] Repositories { get; init; } = Array.Empty<RepositoryMetaData>();

    public HomeworkServiceOptions() { }
}

public class RepositoryMetaData
{
    public string RepositoryName { get; init; } = string.Empty;
    public int[] DiscussionsID { get; init; } = Array.Empty<int>();

}