using CleannetCodeBot.Core;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CleannetCodeBot.Features.Welcome;

public class WelcomeBotClient : IWelcomeBotClient
{
    private readonly IOptionsMonitor<WelcomeBotClientOptions> _optionsMonitor;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly IStickersBotClient _stickersBotClient;

    public WelcomeBotClient(
        ITelegramBotClient telegramBotClient,
        IOptionsMonitor<WelcomeBotClientOptions> optionsMonitor,
        IStickersBotClient stickersBotClient)
    {
        _telegramBotClient = telegramBotClient;
        _optionsMonitor = optionsMonitor;
        _stickersBotClient = stickersBotClient;
    }

    private WelcomeBotClientOptions Options => _optionsMonitor.CurrentValue;

    public async Task SendWelcomeMessageInCommonChatAsync(
        string? username,
        long userId,
        long chatId,
        CancellationToken cancellationToken = default)
    {
        await _stickersBotClient.SendRandomWelcomeStickerFromSetAsync(chatId: chatId, cancellationToken: cancellationToken);

        var me = await _telegramBotClient.GetMeAsync(cancellationToken: cancellationToken);
        var urlToBot = $"https://t.me/{me.Username}";
        var markup = new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl(text: "Ну давай, переходи", url: urlToBot));
        var userLink = username != null
            ? new UserLink(username: username, userId: userId)
            : null;

        await _telegramBotClient.SendTextMessageAsync(
            chatId: chatId,
            text: $@"Привет {userLink?.Value}\. Добро пожаловать в коммьюнити\.
Чтобы удобно ориентироваться в сообществе добавь меня \(бота\) к себе\. Я тебе в деталях все расскажу\.",
            parseMode: ParseMode.MarkdownV2,
            messageThreadId: Options.WelcomeThreadIdByChatId.GetValueOrDefault(chatId),
            replyMarkup: markup,
            cancellationToken: cancellationToken);
    }
}