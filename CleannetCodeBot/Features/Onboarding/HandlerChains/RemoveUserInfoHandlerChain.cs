using CleannetCodeBot.Core;
using CleannetCodeBot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace CleannetCodeBot.Features.Onboarding.HandlerChains;

public class RemoveUserInfoHandlerChain : OnboardingHandlerChainBase
{
    private readonly ILogger<RemoveUserInfoHandlerChain> _logger;

    public RemoveUserInfoHandlerChain(
        IOnboardingBotClient onboardingBotClient,
        IGenericRepository<long, Member> welcomeUserInfoRepository,
        ILogger<RemoveUserInfoHandlerChain> logger) : base(
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
        if (text != OnboardingBotCommands.ClearMyInfoCommand)
            return Errors.NotMatchingStateResult();
        await WelcomeUserInfoRepository.RemoveAsync(key: userId, cancellationToken: cancellationToken);
        await OnboardingBotClient.SendInformationRemovedSuccessfulAsync(
            chatId: user.PersonalChatId!.Value,
            cancellationToken: cancellationToken);
        _logger.LogInformation(message: "{Result}", "Success information remove");
        return Result.Success();
    }
}