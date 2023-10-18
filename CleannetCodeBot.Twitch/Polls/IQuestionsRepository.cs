namespace CleannetCodeBot.Twitch.Polls;

public interface IQuestionsRepository
{
    public PollQuestion GetRandomQuestion();
}