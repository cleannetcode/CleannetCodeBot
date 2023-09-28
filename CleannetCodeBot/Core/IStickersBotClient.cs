namespace CleannetCodeBot.Core;

public interface IStickersBotClient
{
    Task SendRandomWelcomeStickerFromSetAsync(long chatId, CancellationToken cancellationToken);
}