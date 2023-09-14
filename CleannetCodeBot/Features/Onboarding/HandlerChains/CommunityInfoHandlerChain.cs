using CleannetCodeBot.Core;
using CleannetCodeBot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;

namespace CleannetCodeBot.Features.Onboarding.HandlerChains;

public class CommunityInfoHandlerChain : OnboardingHandlerChainBase
{
    public CommunityInfoHandlerChain(
        IOnboardingBotClient onboardingBotClient,
        IGenericRepository<long, Member> welcomeUserInfoRepository)
        : base(
            onboardingBotClient,
            welcomeUserInfoRepository)
    {
    }

    protected override WelcomeUserInfoState TargetState { get; }

    protected override async Task<Result> ProcessUserAsync(long userId, Member user, string text, CancellationToken cancellationToken)
    {
        if (text != OnboardingBotCommands.ShowCommunityInfo)
            return Errors.NotMatchingStateResult();

        await OnboardingBotClient.SendOnboardingMessageAsync(
            username: user.Username,
            userId: user.Id,
            chatId: user.PersonalChatId!.Value,
            cancellationToken: cancellationToken);
        return Result.Success();
    }
}