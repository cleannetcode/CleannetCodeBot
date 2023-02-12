using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace CleannetCode_bot.Features.Forwards;

public class ForwardsHandler : IForwardHandler
{
    private readonly IOptionsMonitor<ForwardsHandlerOptions> _forwardsHandlerOptionsMonitor;
    private readonly ILogger<ForwardsHandler> _logger;

    public ForwardsHandler(
        IOptionsMonitor<ForwardsHandlerOptions> forwardsHandlerOptionsMonitor,
        ILogger<ForwardsHandler> logger)
    {
        _forwardsHandlerOptionsMonitor = forwardsHandlerOptionsMonitor;
        _logger = logger;
    }

    public ForwardsHandlerOptions Options => _forwardsHandlerOptionsMonitor.CurrentValue;

    public async Task HandleAsync(
        long fromChatId,
        int messageId,
        bool isTopicMessage,
        int topicId,
        long senderId,
        ITelegramBotClient botClient,
        CancellationToken ct)
    {
        var logPrefix = $"message: {messageId}, chat: {fromChatId}, topic: {topicId}, handler: {nameof(ForwardsHandler)};";

        _logger.LogDebug("{Prefix:l}", logPrefix);
        if (!isTopicMessage
            || !Options.ChatsWithRestrictedTopicsWithAllowedUsersToWrite.TryGetValue(fromChatId, out var restrictedTopics))
        {
            _logger.LogDebug("{Prefix:l} chat isn't restricted", logPrefix);
            return;
        }

        if (!restrictedTopics.TryGetValue(topicId, out var allowedUsersToWrite))
        {
            _logger.LogDebug("{Prefix:l} chat's topic {Topic} isn't restricted", logPrefix, topicId);
            return;
        }

        if (allowedUsersToWrite.Contains(senderId))
        {
            _logger.LogDebug("{Prefix:l} {User} of chat's topic {Topic} is allowed to write",
                logPrefix,
                senderId,
                topicId);
            return;
        }

        _logger.LogInformation("{Prefix:l} {User} of chat's topic {Topic} isn't allowed to write",
            logPrefix,
            senderId,
            topicId);
        var forwardingMap = Options.ChatsWithTopicsForwardMapping[fromChatId][topicId];
        _logger.LogInformation("{Prefix:l} forward message of user {User} from {Topic} to chat {TargetChat} to topic {TargetTopic}",
            logPrefix,
            senderId,
            topicId,
            forwardingMap.ChatId,
            forwardingMap.ThreadId);

        _logger.LogInformation("{Prefix:l} Forwarding message", logPrefix);
        await botClient.ForwardMessageAsync(forwardingMap.ChatId, fromChatId, messageId, forwardingMap.ThreadId, true, true, ct);

        _logger.LogInformation("{Prefix:l} Deleting message", logPrefix);
        await botClient.DeleteMessageAsync(fromChatId, messageId, ct);
    }
}