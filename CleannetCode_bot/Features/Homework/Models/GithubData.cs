using System.Text.Json.Serialization;

namespace CleannetCode_bot.Features.Homework.Models;

public record CommentNode
{
    [JsonPropertyName("author")]
    public Author? Author { get; init; }

    [JsonPropertyName("body")]
    public string? Body { get; init; }

    [JsonPropertyName("createdAt")]
    public string? CreatedAt { get; init; }

    [JsonPropertyName("databaseId")]
    public int? DatabaseId { get; init; }
}

public record Author
{
    [JsonPropertyName("login")]
    public string? Login { get; init; }
}

public record Commit
{
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("pushedDate")]
    public string? PushedDate { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }
}

public record Stargazers
{
    [JsonPropertyName("totalCount")]
    public int? TotalCount { get; init; }
}
