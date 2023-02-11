using CleannetCode_bot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace CleannetCode_bot.Features.Welcome;

public class WelcomeRequestRemoveInformationHandlerChain : WelcomePrivateHandlerChain
{
    private readonly ILogger<WelcomeGithubAnswerHandlerChain> _logger;

    public WelcomeRequestRemoveInformationHandlerChain(
        IWelcomeBotClient welcomeBotClient,
        IGenericRepository<long, WelcomeUserInfo> welcomeUserInfoRepository,
        ILogger<WelcomeGithubAnswerHandlerChain> logger) : base(
        welcomeBotClient: welcomeBotClient,
        welcomeUserInfoRepository: welcomeUserInfoRepository)
    {
        _logger = logger;
    }

    protected override WelcomeUserInfoState TargetState => WelcomeUserInfoState.Idle;

    protected async override Task<Result> ProcessUserAsync(
        long userId,
        WelcomeUserInfo user,
        string text,
        CancellationToken cancellationToken)
    {
        if (text != WelcomeBotCommandNames.ClearMyInfoCommand)
            return WelcomeHandlerHelpers.NotMatchingStateResult;
        await WelcomeUserInfoRepository.RemoveAsync(key: userId, cancellationToken: cancellationToken);
        await WelcomeBotClient.SendInformationRemovedSuccessfulAsync(
            chatId: user.PersonalChatId!.Value,
            cancellationToken: cancellationToken);
        _logger.LogInformation(message: "{Result}", "Success information remove");
        return Result.Success();
    }
}