using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CleannetCode_bot.Features.Welcome;

public class WelcomeBotClient : IWelcomeBotClient
{
    private readonly BotInfoProvider _botInfoProvider;
    private readonly IOptionsMonitor<WelcomeBotClientOptions> _optionsMonitor;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly IWelcomeStickersBotClient _welcomeStickersBotClient;

    public WelcomeBotClient(
        ITelegramBotClient telegramBotClient,
        IOptionsMonitor<WelcomeBotClientOptions> optionsMonitor,
        IWelcomeStickersBotClient welcomeStickersBotClient,
        BotInfoProvider botInfoProvider)
    {
        _telegramBotClient = telegramBotClient;
        _optionsMonitor = optionsMonitor;
        _welcomeStickersBotClient = welcomeStickersBotClient;
        _botInfoProvider = botInfoProvider;
    }

    private WelcomeBotClientOptions Options => _optionsMonitor.CurrentValue;

    private string UrlToBot => $"https://t.me/{_botInfoProvider.GetMe().Username}";

    public async Task SendWelcomeMessageInCommonChatAsync(
        string username,
        long userId,
        long chatId,
        CancellationToken cancellationToken = default)
    {
        await _welcomeStickersBotClient.SendRandomWelcomeStickerFromSetAsync(chatId: chatId, cancellationToken: cancellationToken);

        var markup = new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl(text: "Ну давай, переходи", url: UrlToBot));

        await _telegramBotClient.SendTextMessageAsync(
            chatId: chatId,
            text: $@"Привет {GetUserLink(username: username, userId: userId)}\. Добро пожаловать в коммьюнити\.
Чтобы удобно ориентироваться в сообществе добавь меня \(бота\) к себе\. Я тебе в деталях все расскажу\.",
            parseMode: ParseMode.MarkdownV2,
            messageThreadId: Options.WelcomeThreadIdByChatId.GetValueOrDefault(chatId),
            replyMarkup: markup,
            cancellationToken: cancellationToken);
    }

    public async Task SendYoutubePromptAsync(
        long chatId,
        CancellationToken cancellationToken = default)
    {
        await _telegramBotClient.SendTextMessageAsync(
            chatId: chatId,
            text: @"Твое имя на Youtube\. Служит для идентификации тебя на стримах: ",
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: cancellationToken);
    }

    public async Task SendYoutubeConfirmedAsync(
        long chatId,
        CancellationToken cancellationToken = default)
    {
        await _telegramBotClient.SendTextMessageAsync(
            chatId: chatId,
            text: @"Успешно заполнен Youtube\.",
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: cancellationToken);
    }

    public async Task SendGithubPromptAsync(
        long chatId,
        CancellationToken cancellationToken = default)
    {
        await _telegramBotClient.SendTextMessageAsync(
            chatId: chatId,
            text:
            @"Твой никнейм на Github \(профиль должен быть доступ через https://github\.com/\<твой ник\>\)\. Будет использоваться для домашек: ",
            parseMode: ParseMode.MarkdownV2,
            disableWebPagePreview: true,
            cancellationToken: cancellationToken);
    }

    public async Task SendGithubConfirmedAsync(
        long chatId,
        CancellationToken cancellationToken = default)
    {
        await _telegramBotClient.SendTextMessageAsync(
            chatId: chatId,
            text: @"Успешно заполнен Github",
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: cancellationToken);
    }

    public async Task SendGithubInvalidProfileAsync(
        long chatId,
        CancellationToken cancellationToken = default)
    {
        await _telegramBotClient.SendTextMessageAsync(
            chatId: chatId,
            text: @"Профиль не найден на Github",
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: cancellationToken);
    }

    public async Task SendInformationAsync(
        long chatId,
        string? username,
        string? githubNick,
        string? youtubeName,
        CancellationToken cancellationToken = default)
    {
        await _telegramBotClient.SendTextMessageAsync(
            chatId: chatId,
            text: $@"Что я знаю о тебе
Имя профиля: {username ?? "Не знаю"},
Github: {githubNick ?? "Не знаю"},
Youtube: {youtubeName ?? "Не знаю"}\.",
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: cancellationToken);
    }

    public async Task SendInformationRemovedSuccessfulAsync(
        long chatId,
        CancellationToken cancellationToken = default)
    {
        var markup = new ReplyKeyboardMarkup(
            new[]
            {
                new KeyboardButton(WelcomeBotCommandNames.StartCommand)
            });
        await _telegramBotClient.SendTextMessageAsync(
            chatId: chatId,
            text: @"Информация удалена",
            replyMarkup: markup,
            cancellationToken: cancellationToken);
    }

    public async Task SendWelcomeMessageInPersonalChatAsync(
        string username,
        long userId,
        long chatId,
        CancellationToken cancellationToken = default)
    {
        await _welcomeStickersBotClient.SendRandomWelcomeStickerFromSetAsync(chatId: chatId, cancellationToken: cancellationToken);
        var markup = new ReplyKeyboardMarkup(
            new[]
            {
                new KeyboardButton(WelcomeBotCommandNames.ChangeGithubInfoCommand),
                new KeyboardButton(WelcomeBotCommandNames.ChangeYoutubeInfoCommand),
                new KeyboardButton(WelcomeBotCommandNames.ClearMyInfoCommand),
                new KeyboardButton(WelcomeBotCommandNames.GetMyInfoCommand)
            });
        await _telegramBotClient.SendTextMessageAsync(
            chatId: chatId,
            text: $@"Снова привет {GetUserLink(username: username, userId: userId)}
В нашем комьюнити ты сможешь найти много чего интересного\.

[Наш основной чат](https://t.me/cleannetcode)
[Стримы тут](https://www.youtube.com/@TrufanovRoman)
[Домашки тут](https://github.com/cleannetcode/Index/discussions)

Чтобы наиболее активно участвовать в сообществе:

\- Получать, сдавать и получать ревью домашек и проектов\.
\- Отслеживать ваш прогресс по изучаению разработки\.

Нам нужна информация о ваших аккаунтов из Telegram, Github и Youtube \(личная персональная информация не требуется\)\.
Telegram мы уже получили хе\-хе
Осталось заполнить информацию о Youtube и Github

{WelcomeBotCommandNames.ChangeGithubInfoCommand} \- показать информацию которую мы собрали
{WelcomeBotCommandNames.ChangeYoutubeInfoCommand} \- удалить твои данные
{WelcomeBotCommandNames.ClearMyInfoCommand} \- указать или изменить имя пользователся на Youtube
{WelcomeBotCommandNames.GetMyInfoCommand} \- указать или изменить ник на Github",
            parseMode: ParseMode.MarkdownV2,
            replyMarkup: markup,
            cancellationToken: cancellationToken);
    }

    private static string GetUserLink(string username, long userId)
    {
        return $"[@{username}](tg://user?id={userId})";
    }
}