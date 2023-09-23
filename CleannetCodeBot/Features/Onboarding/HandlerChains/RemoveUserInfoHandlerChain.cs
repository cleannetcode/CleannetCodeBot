using CleannetCodeBot.Core;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CleannetCodeBot.Features.Onboarding.HandlerChains;

public class RemoveUserInfoHandlerChain : OnboardingHandlerChainBase
{
    private readonly ILogger<RemoveUserInfoHandlerChain> _logger;

    public RemoveUserInfoHandlerChain(
        IOnboardingBotClient onboardingBotClient,
        IMongoDatabase mongoDatabase,
        ILogger<RemoveUserInfoHandlerChain> logger) : base(
        onboardingBotClient: onboardingBotClient,
        mongoDatabase: mongoDatabase)
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
        {
            return Errors.NotMatchingStateResult();
        }

        await MembersCollection.DeleteOneAsync(x => x.Id == userId, cancellationToken: cancellationToken);
        await OnboardingBotClient.SendInformationRemovedSuccessfulAsync(
            chatId: user.PersonalChatId!.Value,
            cancellationToken: cancellationToken);
        _logger.LogInformation(message: "{Result}", "Success information remove");
        return Result.Success();
    }
}