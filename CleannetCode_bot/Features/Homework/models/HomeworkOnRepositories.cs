namespace CleannetCode_bot.Features.Homework;

public record HomeworkOnRepositories
{
    public List<RepositoryInfo> Repositories { get; set; } = new List<RepositoryInfo>();
}

public record RepositoryInfo(
    string Link,
    string? CommitLink,
    string? LastCommitMessage,
    DateTimeOffset? LastUpdate,
    int? CountStars
);