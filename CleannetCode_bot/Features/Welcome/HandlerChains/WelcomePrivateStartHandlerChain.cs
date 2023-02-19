using CleannetCode_bot.Infrastructure;
using CleannetCode_bot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace CleannetCode_bot.Features.Welcome.HandlerChains;

public class WelcomePrivateStartHandlerChain : IHandlerChain
{
    private readonly ILogger<WelcomePrivateStartHandlerChain> _logger;
    private readonly IWelcomeBotClient _welcomeBotClient;
    private readonly IGenericRepository<long, WelcomeUserInfo> _welcomeUserInfoRepository;

    public WelcomePrivateStartHandlerChain(
        IWelcomeBotClient welcomeBotClient,
        IGenericRepository<long, WelcomeUserInfo> welcomeUserInfoRepository,
        ILogger<WelcomePrivateStartHandlerChain> logger)
    {
        _welcomeBotClient = welcomeBotClient;
        _welcomeUserInfoRepository = welcomeUserInfoRepository;
        _logger = logger;
    }

    public int OrderInChain => 0;

    public async Task<Result> HandleAsync(TelegramRequest request, CancellationToken cancellationToken = default)
    {
        var isPrivateChat = request.IsPrivateChat();
        if (isPrivateChat.IsFailure || request.Update is not { Message.From: { } })
        {
            return isPrivateChat;
        }
        
        var text = request.Update.Message?.Text ?? string.Empty;
        if (text != WelcomeBotCommandNames.StartCommand)
        {
            return HandlerResults.NotMatchingType;
        }

        var userId = request.Update.Message?.From?.Id ?? default;
        var user = await _welcomeUserInfoRepository.ReadAsync(
            key: userId,
            cancellationToken: cancellationToken);
        if (user is not null && user.Started)
        {
            return WelcomeHandlerHelpers.NotMatchingStateResult;
        }

        user = request.Update.Message.From.ParseUser() with
        {
            Started = true, 
            PersonalChatId = request.Update.Message.Chat.Id
        };
        return await ProcessUserAsync(user: user, cancellationToken: cancellationToken);
    }

    private async Task<Result> ProcessUserAsync(
        WelcomeUserInfo user,
        CancellationToken cancellationToken)
    {
        await _welcomeUserInfoRepository.SaveAsync(
            key: user.Id,
            entity: user,
            cancellationToken: cancellationToken);
        await _welcomeBotClient.SendWelcomeMessageInPersonalChatAsync(
            userId: user.Id,
            username: user.Username,
            chatId: user.PersonalChatId!.Value,
            cancellationToken: cancellationToken);
        _logger.LogInformation(message: "{Result}", "Success welcome message in personal chat sent");
        return Result.Success();
    }
}