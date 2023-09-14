using CleannetCodeBot.Core;
using CleannetCodeBot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace CleannetCodeBot.Features.Onboarding.HandlerChains;

public class GithubPromptHandlerChain : OnboardingHandlerChainBase
{
    private readonly ILogger<GithubPromptHandlerChain> _logger;

    public GithubPromptHandlerChain(
        IOnboardingBotClient onboardingBotClient,
        IGenericRepository<long, Member> welcomeUserInfoRepository,
        ILogger<GithubPromptHandlerChain> logger) : base(
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
        if (text != OnboardingBotCommands.ChangeGithubInfoCommand)
            return Errors.NotMatchingStateResult();

        await WelcomeUserInfoRepository.SaveAsync(
            key: userId,
            entity: user with
            {
                State = WelcomeUserInfoState.AskingGithub
            },
            cancellationToken: cancellationToken);
        await OnboardingBotClient.SendGithubPromptAsync(
            chatId: user.PersonalChatId!.Value,
            cancellationToken: cancellationToken);
        _logger.LogInformation(message: "{Result}", "Success github prompt");
        return Result.Success();
    }
}