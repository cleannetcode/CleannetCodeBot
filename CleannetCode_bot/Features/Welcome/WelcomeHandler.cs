using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace CleannetCode_bot.Features.Welcome;

public class WelcomeHandler
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
        WriteIndented = true
    };

    private readonly IConfiguration _config;
    private readonly ILogger<WelcomeHandler> _logger;
    private readonly ITelegramBotClient _client;

    private readonly ConcurrentDictionary<long, SemaphoreSlim> _writeSemaphoreSlims = new();
    private readonly ConcurrentDictionary<long, SemaphoreSlim> _workWithStateSemaphoreSlims = new();

    private static readonly Dictionary<int, Position> Positions = new()
    {
        { 1, new(1, "Учусь") },
        { 2, new(2, "Стажируюсь") },
        { 3, new(3, "Джуниор") },
        { 4, new(4, "Мидл") },
        { 5, new(5, "Сеньер") },
        { 6, new(6, "Лид") },
    };

    public WelcomeHandler(ITelegramBotClient client, IConfiguration config, ILogger<WelcomeHandler> logger)
    {
        _client = client;
        _config = config;
        _logger = logger;
    }

    public async Task HandleAnswersAsync(Message message)
    {
        if (message.From is null
            || message.ReplyToMessage is null
            || message.Text is null)
            return;

        _ = await LockAsync(message.From.Id,
            async () =>
            {
                var user = await ReadAsync(message.From.Id);
                if (user is null)
                    return true;

                switch (user.State)
                {
                    case State.Github when message.ReplyToMessage.MessageId == user.GithubMessageId:
                        await HandleGithubAnswerAsync(message.Text, message.MessageId, message.Chat.Id, user);
                        break;
                    case State.Youtube when message.ReplyToMessage.MessageId == user.YoutubeMessageId:
                        await HandleYoutubeAnswerAsync(message.Text, message.MessageId, message.Chat.Id, user);
                        break;
                    case State.Position when message.ReplyToMessage.MessageId == user.PositionMessageId:
                        await HandlePositionAnswerAsync(message.Text, message.MessageId, message.Chat.Id, user);
                        break;
                    case State.None:
                    case State.End:
                    default:
                        break;
                }
                return true;
            },
            _workWithStateSemaphoreSlims);
    }

    private async Task HandleGithubAnswerAsync(string text, int messageId, long chatId, WelcomeUserInfo user)
    {
        var message = await _client.SendTextMessageAsync(chatId,
            $"@{user.Username}, А твой ник на ютьюбе (ответь на это сообщение):",
            replyToMessageId: messageId);
        user = user with { GithubNick = text, YoutubeMessageId = message.MessageId, State = State.Youtube };
        await SaveAsync(user);
    }

    private async Task HandleYoutubeAnswerAsync(string text, int messageId, long chatId, WelcomeUserInfo user)
    {
        var message = await _client.SendTextMessageAsync(chatId,
            $"@{user.Username}, А уровень (Учусь, Стажируюсь, Джуниор, Мидл, Сеньер, Лид) (ответь на это сообщение):",
            replyToMessageId: messageId);
        user = user with { YoutubeNick = text, PositionMessageId = message.MessageId, State = State.Position };
        await SaveAsync(user);
    }

    private async Task HandlePositionAnswerAsync(string positionAnswer, int messageId, long chatId, WelcomeUserInfo user)
    {
        positionAnswer = positionAnswer.ToLower().Trim();
        var selected = Positions.Select(x => x.Value)
            .FirstOrDefault(x => x.Name.Equals(positionAnswer, StringComparison.CurrentCultureIgnoreCase));
        if (selected is not null)
        {
            user = user with { Position = selected, PositionId = selected.Id, State = State.End };
            await SaveAsync(user);
            await _client.SendTextMessageAsync(chatId,
                $"@{user.Username}, Анкета успешно собрана! 👌😁👍",
                replyToMessageId: messageId);
            _logger.LogDebug("Finish taking user survey {Position}", selected);
        }
        else
        {
            _logger.LogDebug("Trying to recheck position {Position}", positionAnswer);
            var message = await _client.SendTextMessageAsync(chatId,
                $"@{user.Username}, Не понял 🤨. Твой уровень из списка (Учусь, Стажируюсь, Джуниор, Мидл, Сеньер, Лид) (ответь на это сообщение):",
                replyToMessageId: messageId);
            user = user with { PositionMessageId = message.MessageId };
            await SaveAsync(user);
        }
    }

    private async Task SaveAsync(WelcomeUserInfo user)
    {
        _ = await LockAsync(user.Id,
            async () =>
            {
                var fileName = GetFileName(user.Id);
                var directoryName = Path.GetDirectoryName(fileName)!;
                if (!Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);
                await using var fileStream = File.OpenWrite(fileName);
                fileStream.SetLength(0);
                await JsonSerializer.SerializeAsync(fileStream, user, _jsonSerializerOptions);
                _logger.LogDebug("Saved user {User}", user);
                return true;
            },
            _writeSemaphoreSlims);
    }

    private async Task<WelcomeUserInfo?> ReadAsync(long id)
    {
        return await LockAsync(id,
            async () =>
            {
                var fileName = GetFileName(id);
                if (!File.Exists(fileName)) return null;
                await using var fileStream = File.OpenRead(fileName);
                var user = await JsonSerializer.DeserializeAsync<WelcomeUserInfo>(fileStream, _jsonSerializerOptions);
                _logger.LogDebug("Read user {User}", user);
                return user;
            },
            _writeSemaphoreSlims);
    }

    private async Task<T> LockAsync<T>(
        long id,
        Func<Task<T>> action,
        ConcurrentDictionary<long, SemaphoreSlim> concurrentDictionary,
        [CallerArgumentExpression(nameof(concurrentDictionary))]
        string? lockName = default)
    {
        _logger.LogDebug("Lock {LockName} element with id {Id}", id, lockName ?? "[no lock name]");
        var semaphore = concurrentDictionary.GetOrAdd(id, new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try { return await action(); }
        finally
        {
            semaphore.Release();
            _logger.LogDebug("Unlock {LockName} element with id {Id}", id, lockName ?? "[no lock name]");
        }
    }

    private string GetFileName(long id)
    {
        var basePath = _config["WelcomesPath"] ?? "Welcomes";
        return Path.Combine(basePath, $"{id.ToString()}.json");
    }

    public async Task HandleChatMember(User member, long chatId)
    {
        var fromId = member.Id;
        var user = new WelcomeUserInfo(fromId,
            member.Username ?? $"{member.FirstName}{(string.IsNullOrEmpty(member.LastName) ? string.Empty : " " + member.LastName)}",
            member.FirstName,
            member.LastName ?? string.Empty,
            State: State.Github);
        var githubMessage = await _client.SendTextMessageAsync(chatId,
            entities: new[] { new MessageEntity() { Type = MessageEntityType.TextMention, User = member } },
            text: $"Привет! 👀👀👀 @{user.Username}. Твой никнейм в гитхаб (ответь на это сообщение):");
        user = user with { GithubMessageId = githubMessage.MessageId };
        await SaveAsync(user);
    }
}