using CleannetCode_bot.Infrastructure;
using CleannetCode_bot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CleannetCode_bot.Features.Welcome.HandlerChains;

public class WelcomeMemberJoinHandlerChain : IHandlerChain
{
    private readonly ILogger<WelcomeMemberJoinHandlerChain> _logger;
    private readonly IWelcomeBotClient _welcomeBotClient;
    private readonly IGenericRepository<long, WelcomeUserInfo> _welcomeUserInfoRepository;

    public WelcomeMemberJoinHandlerChain(
        IWelcomeBotClient welcomeBotClient,
        IGenericRepository<long, WelcomeUserInfo> welcomeUserInfoRepository,
        ILogger<WelcomeMemberJoinHandlerChain> logger)
    {
        _welcomeBotClient = welcomeBotClient;
        _welcomeUserInfoRepository = welcomeUserInfoRepository;
        _logger = logger;
    }

    public int OrderInChain => 0;

    public async Task<Result> HandleAsync(TelegramRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Update.ChatMember is { NewChatMember: { Status: ChatMemberStatus.Member, User: {} } } chatMember)
        {
            await ProcessUserAsync(chatId: chatMember.Chat.Id, member: chatMember.NewChatMember.User, cancellationToken: cancellationToken);
            return Result.Success();
        }
        if (request.Update.Message is { NewChatMembers: {} } message && message.NewChatMembers.Any())
        {
            var results = new List<Result>(message.NewChatMembers.Length);
            foreach (var user in message.NewChatMembers)
            {
                results.Add(await ProcessUserAsync(chatId: message.Chat.Id, member: user, cancellationToken: cancellationToken));
            }
            return Result.Combine(results);
        }
        return HandlerResults.NotMatchingType;
    }

    private async Task<Result> ProcessUserAsync(
        long chatId,
        User member,
        CancellationToken cancellationToken)
    {
        var user = member.ParseUser();

        await _welcomeUserInfoRepository.SaveAsync(
            key: user.Id,
            entity: user,
            cancellationToken: cancellationToken);
        await _welcomeBotClient.SendWelcomeMessageInCommonChatAsync(
            userId: user.Id,
            username: user.Username,
            chatId: chatId,
            cancellationToken: cancellationToken);
        _logger.LogInformation(message: "{Result}", "Success welcome message in common chat sent");
        return Result.Success();
    }
}