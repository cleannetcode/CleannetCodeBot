namespace CleannetCode_bot.Features.Homework;

public record CommentsDiscussions
{
    public Dictionary<string, Comment[]> Discussions { get; init; } = new Dictionary<string, Comment[]>();
}

public record Comment(
    string Author,
    string Message,
    DateTimeOffset DateCreate,
    int CommentId
);