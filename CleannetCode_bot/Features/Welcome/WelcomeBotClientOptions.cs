namespace CleannetCode_bot.Features.Welcome;

public class WelcomeBotClientOptions
{
    public const string Section = nameof(WelcomeBotClientOptions);

    public string StickerSet { get; set; } = string.Empty;

    public Dictionary<long, int?> WelcomeThreadIdByChatId { get; set; } = new();
}