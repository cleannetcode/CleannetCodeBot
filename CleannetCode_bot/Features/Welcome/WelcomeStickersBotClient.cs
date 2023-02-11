using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CleannetCode_bot.Features.Welcome;

public class WelcomeStickersBotClient : IWelcomeStickersBotClient
{
    private readonly IOptionsMonitor<WelcomeBotClientOptions> _optionsMonitor;
    private readonly ITelegramBotClient _telegramBotClient;
    private string[] _stickersPickedToRandomSending = Array.Empty<string>();

    public WelcomeStickersBotClient(ITelegramBotClient telegramBotClient, IOptionsMonitor<WelcomeBotClientOptions> optionsMonitor)
    {
        _telegramBotClient = telegramBotClient;
        _optionsMonitor = optionsMonitor;
    }

    private WelcomeBotClientOptions Options => _optionsMonitor.CurrentValue;

    public async Task SendRandomWelcomeStickerFromSetAsync(long chatId, CancellationToken cancellationToken)
    {
        if (!_stickersPickedToRandomSending.Any())
        {
            var set = await _telegramBotClient.GetStickerSetAsync(name: Options.StickerSet, cancellationToken: cancellationToken);
            _stickersPickedToRandomSending = set.Stickers.Select(x => x.FileId).ToArray();
        }

        var index = Random.Shared.Next(minValue: 0, maxValue: _stickersPickedToRandomSending.Length);
        var randomPickedSticker = _stickersPickedToRandomSending[
            index];
        await _telegramBotClient.SendStickerAsync(
            chatId: chatId,
            sticker: new InputFileId(randomPickedSticker),
            messageThreadId: Options.WelcomeThreadIdByChatId.GetValueOrDefault(chatId),
            disableNotification: true,
            cancellationToken: cancellationToken);
    }
}