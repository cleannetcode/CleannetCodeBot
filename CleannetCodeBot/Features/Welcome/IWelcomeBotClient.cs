namespace CleannetCodeBot.Features.Welcome;

public interface IWelcomeBotClient
{
    Task SendWelcomeMessageInCommonChatAsync(
        string username,
        long userId,
        long chatId,
        CancellationToken cancellationToken = default);
}