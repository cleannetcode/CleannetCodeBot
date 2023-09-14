using CleannetCodeBot.Core;
using CleannetCodeBot.Infrastructure;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CleannetCodeBot.Features.Welcome.HandlerChains;

public class MemberJoinHandlerChain : IHandlerChain
{
    private readonly ILogger<MemberJoinHandlerChain> _logger;
    private readonly IWelcomeBotClient _welcomeBotClient;
    private readonly IMongoCollection<Member> _membersCollection;

    public MemberJoinHandlerChain(
        IWelcomeBotClient welcomeBotClient,
        IMongoDatabase mongoDatabase,
        ILogger<MemberJoinHandlerChain> logger)
    {
        _welcomeBotClient = welcomeBotClient;
        _membersCollection = mongoDatabase.GetCollection<Member>(Member.CollectionName);
        _logger = logger;
    }

    public int OrderInChain => 0;

    public async Task<Result> HandleAsync(TelegramRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Update.ChatMember is { NewChatMember: { Status: ChatMemberStatus.Member, User: { } } } chatMember)
        {
            await ProcessUserAsync(chatId: chatMember.Chat.Id, user: chatMember.NewChatMember.User, cancellationToken: cancellationToken);
            return Result.Success();
        }

        if (request.Update.Message is { NewChatMembers: { } } message && message.NewChatMembers.Any())
        {
            var results = new List<Result>(message.NewChatMembers.Length);
            foreach (var user in message.NewChatMembers)
            {
                results.Add(await ProcessUserAsync(chatId: message.Chat.Id, user: user, cancellationToken: cancellationToken));
            }

            return Result.Combine(results);
        }

        return HandlerResults.NotMatchingType;
    }

    private async Task<Result> ProcessUserAsync(
        long chatId,
        User user,
        CancellationToken cancellationToken)
    {
        var member = new Member(
            id: user.Id,
            username: user.Username,
            firstName: user.FirstName,
            lastName: user.LastName ?? "",
            started: true,
            personalChatId: chatId
        );

        await _membersCollection.InsertOneAsync(member, cancellationToken: cancellationToken);
        await _welcomeBotClient.SendWelcomeMessageInCommonChatAsync(
            userId: user.Id,
            username: user.Username,
            chatId: chatId,
            cancellationToken: cancellationToken);
        _logger.LogInformation(message: "{Result}", "Success welcome message in common chat sent");
        return Result.Success();
    }
}