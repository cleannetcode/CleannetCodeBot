using CleannetCodeBot.Core;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CleannetCodeBot.Features.Onboarding.HandlerChains;

public class YoutubeAnswerHandlerChain : OnboardingHandlerChainBase
{
    private readonly ILogger<YoutubeAnswerHandlerChain> _logger;

    public YoutubeAnswerHandlerChain(
        IOnboardingBotClient onboardingBotClient,
        IMongoDatabase mongoDatabase,
        ILogger<YoutubeAnswerHandlerChain> logger) : base(
        onboardingBotClient: onboardingBotClient,
        mongoDatabase: mongoDatabase)
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
        var update = Builders<Member>.Update
            .Set(x => x.YoutubeName, text)
            .Set(x => x.State, WelcomeUserInfoState.Idle);

        await MembersCollection.UpdateOneAsync(
            x => x.Id == userId,
            update: update,
            cancellationToken: cancellationToken);
        await OnboardingBotClient.SendYoutubeConfirmedAsync(
            chatId: user.PersonalChatId!.Value,
            cancellationToken: cancellationToken);
        _logger.LogInformation(message: "{Result}", "Success youtube answer handling");
        return Result.Success();
    }
}