using CleannetCode_bot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace CleannetCode_bot.Features.Welcome.HandlerChains;

public class WelcomeGithubPromptHandlerChain : WelcomePrivateHandlerChain
{
    private readonly ILogger<WelcomeGithubPromptHandlerChain> _logger;

    public WelcomeGithubPromptHandlerChain(
        IWelcomeBotClient welcomeBotClient,
        IGenericRepository<long, WelcomeUserInfo> welcomeUserInfoRepository,
        ILogger<WelcomeGithubPromptHandlerChain> logger) : base(
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
        if (text != WelcomeBotCommandNames.ChangeGithubInfoCommand)
            return WelcomeHandlerHelpers.NotMatchingStateResult;

        await WelcomeUserInfoRepository.SaveAsync(
            key: userId,
            entity: user with
            {
                State = WelcomeUserInfoState.AskingGithub
            },
            cancellationToken: cancellationToken);
        await WelcomeBotClient.SendGithubPromptAsync(
            chatId: user.PersonalChatId!.Value,
            cancellationToken: cancellationToken);
        _logger.LogInformation(message: "{Result}", "Success github prompt");
        return Result.Success();
    }
}