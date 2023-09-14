using CleannetCodeBot.Core;
using CleannetCodeBot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace CleannetCodeBot.Features.Onboarding.HandlerChains;

public class GetUserInfoHandlerChain : OnboardingHandlerChainBase
{
    private readonly ILogger<GetUserInfoHandlerChain> _logger;

    public GetUserInfoHandlerChain(
        IOnboardingBotClient onboardingBotClient,
        IGenericRepository<long, Member> welcomeUserInfoRepository,
        ILogger<GetUserInfoHandlerChain> logger) : base(
        onboardingBotClient: onboardingBotClient,
        welcomeUserInfoRepository: welcomeUserInfoRepository)
    {
        _logger = logger;
    }

    protected override WelcomeUserInfoState TargetState => WelcomeUserInfoState.Idle;

    protected override async Task<Result> ProcessUserAsync(
        long userId,
        Member user,
        string text,
        CancellationToken cancellationToken)
    {
        if (text != OnboardingBotCommands.GetMyInfoCommand)
            return Errors.NotMatchingStateResult();
        await OnboardingBotClient.SendInformationAsync(
            chatId: user.PersonalChatId!.Value,
            username: user.Username,
            githubNick: user.GithubNick,
            youtubeName: user.YoutubeName,
            cancellationToken: cancellationToken);
        _logger.LogInformation(message: "{Result}", "Success return information");
        return Result.Success();
    }
}