namespace CleannetCodeBot.Twitch.Polls;

public record Vote(string QuestionId, string UserId, string Answer, bool IsAnswerCorrect);