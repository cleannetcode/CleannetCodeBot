using CleannetCodeBot.Core;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CleannetCodeBot.Features.Onboarding.HandlerChains;

public class GithubPromptHandlerChain : OnboardingHandlerChainBase
{
    private readonly ILogger<GithubPromptHandlerChain> _logger;

    public GithubPromptHandlerChain(
        IOnboardingBotClient onboardingBotClient,
        IMongoDatabase mongoDatabase,
        ILogger<GithubPromptHandlerChain> logger) : base(
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
        if (text != OnboardingBotCommands.ChangeGithubInfoCommand)
            return Errors.NotMatchingStateResult();

        var update = Builders<Member>.Update
            .Set(x => x.State, WelcomeUserInfoState.AskingGithub);

        await MembersCollection.UpdateOneAsync(
            x => x.Id == userId,
            update: update,
            cancellationToken: cancellationToken);
        await OnboardingBotClient.SendGithubPromptAsync(
            chatId: user.PersonalChatId!.Value,
            cancellationToken: cancellationToken);
        _logger.LogInformation(message: "{Result}", "Success github prompt");
        return Result.Success();
    }
}