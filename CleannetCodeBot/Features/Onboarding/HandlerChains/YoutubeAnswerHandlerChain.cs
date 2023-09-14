using CleannetCodeBot.Core;
using CleannetCodeBot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace CleannetCodeBot.Features.Onboarding.HandlerChains;

public class YoutubeAnswerHandlerChain : OnboardingHandlerChainBase
{
    private readonly ILogger<YoutubeAnswerHandlerChain> _logger;

    public YoutubeAnswerHandlerChain(
        IOnboardingBotClient onboardingBotClient,
        IGenericRepository<long, Member> welcomeUserInfoRepository,
        ILogger<YoutubeAnswerHandlerChain> logger) : base(
        onboardingBotClient: onboardingBotClient,
        welcomeUserInfoRepository: welcomeUserInfoRepository)
    {
        _logger = logger;
    }

    protected override WelcomeUserInfoState TargetState => WelcomeUserInfoState.AskingYoutube;

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
                YoutubeName = text, State = WelcomeUserInfoState.Idle
            },
            cancellationToken: cancellationToken);
        await OnboardingBotClient.SendYoutubeConfirmedAsync(
            chatId: user.PersonalChatId!.Value,
            cancellationToken: cancellationToken);
        _logger.LogInformation(message: "{Result}", "Success youtube answer handling");
        return Result.Success();
    }
}