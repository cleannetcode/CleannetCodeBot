using CleannetCode_bot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace CleannetCode_bot.Features.Welcome;

public class WelcomeRequestInformationHandlerChain : WelcomePrivateHandlerChain
{
    private readonly ILogger<WelcomeGithubAnswerHandlerChain> _logger;

    public WelcomeRequestInformationHandlerChain(
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
        if (text != WelcomeBotCommandNames.GetMyInfoCommand)
            return WelcomeHandlerHelpers.NotMatchingStateResult;
        await WelcomeBotClient.SendInformationAsync(
            chatId: user.PersonalChatId!.Value,
            username: user.Username,
            githubNick: user.GithubNick,
            youtubeName: user.YoutubeName,
            cancellationToken: cancellationToken);
        _logger.LogInformation(message: "{Result}", "Success return information");
        return Result.Success();
    }
}