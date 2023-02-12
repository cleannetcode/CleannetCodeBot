using CleannetCode_bot.Features.Forwards;
using CleannetCode_bot.Features.Statistics;
using CleannetCode_bot.Features.Welcome;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Payments;

namespace CleannetCode_bot;

public class Handlers
{
    private readonly IStorageService _storage;
    private readonly WelcomeHandler _welcomeHandler;
    private readonly IForwardHandler _forwardHandler;
    private readonly ITelegramBotClient _telegramBotClient;

    public Handlers(
        IStorageService storage,
        WelcomeHandler welcomeHandler,
        IForwardHandler forwardHandler,
        ITelegramBotClient telegramBotClient)
    {
        _storage = storage;
        _welcomeHandler = welcomeHandler;
        _forwardHandler = forwardHandler;
        _telegramBotClient = telegramBotClient;
    }
    public Task CallbackQueryAsync(CallbackQuery? callbackQuery, CancellationToken cts)
    {
        if (callbackQuery is null) { return Task.CompletedTask; }
        _storage.AddObject(callbackQuery, typeof(CallbackQuery), "callbackQuery", cts);
        return Task.CompletedTask;
    }

    public Task ChannelPostAsync(Message? channelPost, CancellationToken cts)
    {
        if (channelPost is null) { return Task.CompletedTask; }
        _storage.AddObject(channelPost, typeof(Message), "channelPost", cts);
        return Task.CompletedTask;
    }

    public Task ChatJoinRequestAsync(ChatJoinRequest? chatJoinRequest, CancellationToken cts)
    {
        if (chatJoinRequest is null) { return Task.CompletedTask; }
        _storage.AddObject(chatJoinRequest, typeof(ChatJoinRequest), "chatJoinRequest", cts);
        return Task.CompletedTask;
    }

    public Task ChatMemberAsync(ChatMemberUpdated? chatMember, CancellationToken cts)
    {
        if (chatMember is null) { return Task.CompletedTask; }
        _storage.AddObject(chatMember, typeof(ChatMemberUpdated), "chatMember", cts);
        return Task.CompletedTask;
    }

    public Task ChosenInlineResultAsync(ChosenInlineResult? chosenInlineResult, CancellationToken cts)
    {
        if (chosenInlineResult is null) { return Task.CompletedTask; }
        _storage.AddObject(chosenInlineResult, typeof(ChosenInlineResult), "chosenInlineResult", cts);
        return Task.CompletedTask;
    }

    public Task EditedChannelPostAsync(Message? editedChannelPost, CancellationToken cts)
    {
        if (editedChannelPost is null) { return Task.CompletedTask; }
        _storage.AddObject(editedChannelPost, typeof(Message), "editedChannelPost", cts);
        return Task.CompletedTask;
    }

    public Task EditedMessageAsync(Message? editedMessage, CancellationToken cts)
    {
        if (editedMessage is null) { return Task.CompletedTask; }
        _storage.AddObject(editedMessage, typeof(Message), "editedMessage", cts);
        return Task.CompletedTask;
    }

    public Task InlineQueryAsync(InlineQuery? inlineQuery, CancellationToken cts)
    {
        if (inlineQuery is null) { return Task.CompletedTask; }
        _storage.AddObject(inlineQuery, typeof(InlineQuery), "inlineQuery", cts);
        return Task.CompletedTask;
    }

    public async Task MessageAsync(Message? message, CancellationToken cts)
    {
        if (message is null) { return; }
        await _storage.AddObject(message, typeof(Message), "Message", cts);
        await _welcomeHandler.HandleAnswersAsync(message);
        if (message.From is not null && message.Text == "/welcome")
        {
            await _welcomeHandler.HandleChatMember(message.From, message.Chat.Id);
        }
        if (message.From is null) { return; }
        await _forwardHandler.HandleAsync(
            message.Chat.Id,
            message.MessageId,
            message.IsTopicMessage.GetValueOrDefault(),
            message.MessageThreadId.GetValueOrDefault(),
            message.From.Id,
            _telegramBotClient,
            cts);
    }

    public Task MyChatMemberAsync(ChatMemberUpdated? myChatMember, CancellationToken cts)
    {
        if (myChatMember is null) { return Task.CompletedTask; }
        _storage.AddObject(myChatMember, typeof(ChatMemberUpdated), "myChatMember", cts);
        return Task.CompletedTask;
    }

    public Task PollAnswerAsync(PollAnswer? pollAnswer, CancellationToken cts)
    {
        if (pollAnswer is null) { return Task.CompletedTask; }
        _storage.AddObject(pollAnswer, typeof(PollAnswer), "pollAnswer", cts);
        return Task.CompletedTask;
    }

    public Task PollAsync(Poll? poll, CancellationToken cts)
    {
        if (poll is null) { return Task.CompletedTask; }
        _storage.AddObject(poll, typeof(Poll), "poll", cts);
        return Task.CompletedTask;
    }

    public Task PreCheckoutQueryAsync(PreCheckoutQuery? preCheckoutQuery, CancellationToken cts)
    {
        return Task.CompletedTask;
    }

    public Task ShippingQueryAsync(ShippingQuery? shippingQuery, CancellationToken cts)
    {
        return Task.CompletedTask;
    }

    public Task UnknownAsync(Update update, CancellationToken cts)
    {
        _storage.AddObject(update, typeof(Update), "Unknown", cts);
        return Task.CompletedTask;
    }
}