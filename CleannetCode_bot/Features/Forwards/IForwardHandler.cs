using Telegram.Bot;

namespace CleannetCode_bot.Features.Forwards;

public interface IForwardHandler
{
    Task HandleAsync(
        long fromChatId,
        int messageId,
        bool isTopicMessage,
        int topicId,
        long senderId,
        ITelegramBotClient botClient,
        CancellationToken ct);
}