namespace CleannetCodeBot.Twitch.Polls;

public record QuestionAnswer(string Key, string Content);

public class PollQuestion
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Content { get; init; }
    
    public QuestionAnswer[] Answers { get; init; }
    
    public string CorrectAnswerKey { get; init; }


    public bool IsAnswerExists(string answer) =>
        Answers.Any(x => string.Equals(x.Key, answer, StringComparison.OrdinalIgnoreCase));

    public bool IsAnswerCorrect(string answer) =>
        string.Equals(CorrectAnswerKey, answer, StringComparison.OrdinalIgnoreCase);

    public override string ToString()
    {
        return Content + "  " + string.Join(' ', Answers.Select(x => x.Key + ")" + x.Content));
    }
}