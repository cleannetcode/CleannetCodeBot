using CleannetCode_bot.Infrastructure;
using CSharpFunctionalExtensions;
using Telegram.Bot;

namespace CleannetCode_bot.Features.Forwards;

public class ForwardHandlerChain : IHandlerChain
{
    private IForwardsService _forwardsService;
    private readonly ITelegramBotClient _telegramBotClient;

    public ForwardHandlerChain(
        IForwardsService forwardsService,
        ITelegramBotClient telegramBotClient)
    {
        _forwardsService = forwardsService;
        _telegramBotClient = telegramBotClient;
    }

    public int OrderInChain => -1;

    public async Task<Result> HandleAsync(
        TelegramRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Update.Message is not { From: {} } message)
            return HandlerResults.NotMatchingType;
        await _forwardsService.HandleAsync(fromChatId: message.Chat.Id,
            messageId: message.MessageId,
            isTopicMessage: message.IsTopicMessage.GetValueOrDefault(false),
            topicId: message.MessageThreadId.GetValueOrDefault(-1),
            senderId: message.From.Id,
            botClient: _telegramBotClient,
            ct: cancellationToken);
        return Result.Success();
    }
}
