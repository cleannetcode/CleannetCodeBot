public record DiscussionMessages
{
    public string Author { get; init; }
    public string Message { get; init; }
    public DateTimeOffset DatetimeCreateNode { get; init; }
    public DiscussionMessages(string author, string message, DateTimeOffset datetimeCreateNode)
    {
        Author = author;
        Message = message;
        DatetimeCreateNode = datetimeCreateNode;
    }
}