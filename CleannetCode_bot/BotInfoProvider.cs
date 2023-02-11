using Telegram.Bot;
using Telegram.Bot.Types;

namespace CleannetCode_bot;

public class BotInfoProvider
{
    private readonly ITelegramBotClient _client;
    private User _me = new();

    public BotInfoProvider(ITelegramBotClient client)
    {
        _client = client;
    }

    public User GetMe()
    {
        return _me;
    }

    public async Task InitAsync()
    {
        _me = await _client.GetMeAsync();
    }
}