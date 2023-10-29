using CleannetCodeBot.Twitch.Polls;

namespace CleannetCodeBot.Twitch.Infrastructure;

public class QuestionsRepository : IQuestionsRepository
{
    private List<PollQuestion> _questions = new List<PollQuestion>()
    {
        new PollQuestion() 
        { 
            Content = "Вопрос 1", 
            Answers = new[]
            {
                new QuestionAnswer("1", "Вариант 1"),
                new QuestionAnswer("2", "Вариант 2"),
                new QuestionAnswer("3", "Вариант 3"),
                new QuestionAnswer("4", "Вариант 4")
            },
            CorrectAnswerKey = "1"
        }
    };
    
    public PollQuestion GetRandomQuestion()
    {
        return _questions.First();
    }
}