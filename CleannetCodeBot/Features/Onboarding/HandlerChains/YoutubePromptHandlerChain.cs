using CleannetCodeBot.Core;
using CleannetCodeBot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace CleannetCodeBot.Features.Onboarding.HandlerChains;

public class YoutubePromptHandlerChain : OnboardingHandlerChainBase
{
    private readonly ILogger<YoutubePromptHandlerChain> _logger;

    public YoutubePromptHandlerChain(
        IOnboardingBotClient onboardingBotClient,
        IGenericRepository<long, Member> welcomeUserInfoRepository,
        ILogger<YoutubePromptHandlerChain> logger) : base(
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
        if (text != OnboardingBotCommands.ChangeYoutubeInfoCommand)
            return Errors.NotMatchingStateResult();

        await WelcomeUserInfoRepository.SaveAsync(
            key: userId,
            entity: user with
            {
                State = WelcomeUserInfoState.AskingYoutube
            },
            cancellationToken: cancellationToken);
        await OnboardingBotClient.SendYoutubePromptAsync(
            chatId: user.PersonalChatId!.Value,
            cancellationToken: cancellationToken);
        // TODO: Сделать проверку на существование профиля в Github
        _logger.LogInformation(message: "{Result}", "Success youtube prompt");
        return Result.Success();
    }
}