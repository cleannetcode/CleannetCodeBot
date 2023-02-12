using Telegram.Bot.Types;

namespace CleannetCode_bot.Infrastructure;
public class TelegramRequest
{
    public TelegramRequest(Update update)
    {
        Update = update;
    }

    public Update Update { get; }
}