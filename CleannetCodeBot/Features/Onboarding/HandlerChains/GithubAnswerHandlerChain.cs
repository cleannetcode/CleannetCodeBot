using CleannetCodeBot.Core;
using CleannetCodeBot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace CleannetCodeBot.Features.Onboarding.HandlerChains;

public class GithubAnswerHandlerChain : OnboardingHandlerChainBase
{
    private readonly ILogger<GithubAnswerHandlerChain> _logger;

    public GithubAnswerHandlerChain(
        IOnboardingBotClient onboardingBotClient,
        IGenericRepository<long, Member> welcomeUserInfoRepository,
        ILogger<GithubAnswerHandlerChain> logger) : base(
        onboardingBotClient: onboardingBotClient,
        welcomeUserInfoRepository: welcomeUserInfoRepository)
    {
        _logger = logger;
    }

    protected override WelcomeUserInfoState TargetState => WelcomeUserInfoState.AskingGithub;

    protected override async Task<Result> ProcessUserAsync(
        long userId,
        Member user,
        string text,
        CancellationToken cancellationToken)
    {
        await WelcomeUserInfoRepository.SaveAsync(
            key: userId,
            entity: user with
            {
                GithubNick = text, State = WelcomeUserInfoState.Idle
            },
            cancellationToken: cancellationToken);
        await OnboardingBotClient.SendGithubConfirmedAsync(
            chatId: user.PersonalChatId!.Value,
            cancellationToken: cancellationToken);
        // TODO: Сделать проверку на существование профиля в Github
        _logger.LogInformation(message: "{Result}", "Success github answer handling");
        return Result.Success();
    }
}