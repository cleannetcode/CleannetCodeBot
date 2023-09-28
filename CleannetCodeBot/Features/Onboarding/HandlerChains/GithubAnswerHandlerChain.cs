using CleannetCodeBot.Core;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CleannetCodeBot.Features.Onboarding.HandlerChains;

public class GithubAnswerHandlerChain : OnboardingHandlerChainBase
{
    private readonly ILogger<GithubAnswerHandlerChain> _logger;

    public GithubAnswerHandlerChain(
        IOnboardingBotClient onboardingBotClient,
        IMongoDatabase mongoDatabase,
        ILogger<GithubAnswerHandlerChain> logger) : base(
        onboardingBotClient: onboardingBotClient,
        mongoDatabase: mongoDatabase)
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
        var update = Builders<Member>.Update
            .Set(x => x.GithubNick, text)
            .Set(x => x.State, WelcomeUserInfoState.Idle);

        await MembersCollection.UpdateOneAsync(
            x => x.Id == userId,
            update: update,
            cancellationToken: cancellationToken);
        await OnboardingBotClient.SendGithubConfirmedAsync(
            chatId: user.PersonalChatId!.Value,
            cancellationToken: cancellationToken);
        // TODO: Сделать проверку на существование профиля в Github
        _logger.LogInformation(message: "{Result}", "Success github answer handling");
        return Result.Success();
    }
}