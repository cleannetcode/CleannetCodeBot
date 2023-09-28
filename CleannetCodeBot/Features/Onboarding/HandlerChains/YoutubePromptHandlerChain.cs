using CleannetCodeBot.Core;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CleannetCodeBot.Features.Onboarding.HandlerChains;

public class YoutubePromptHandlerChain : OnboardingHandlerChainBase
{
    private readonly ILogger<YoutubePromptHandlerChain> _logger;

    public YoutubePromptHandlerChain(
        IOnboardingBotClient onboardingBotClient,
        IMongoDatabase mongoDatabase,
        ILogger<YoutubePromptHandlerChain> logger) : base(
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
        if (text != OnboardingBotCommands.ChangeYoutubeInfoCommand)
            return Errors.NotMatchingStateResult();
        
        var update = Builders<Member>.Update
            .Set(x => x.State, WelcomeUserInfoState.AskingYoutube);

        await MembersCollection.UpdateOneAsync(
            x => x.Id == userId,
            update: update,
            cancellationToken: cancellationToken);
        await OnboardingBotClient.SendYoutubePromptAsync(
            chatId: user.PersonalChatId!.Value,
            cancellationToken: cancellationToken);
        // TODO: Сделать проверку на существование профиля в YouTube
        _logger.LogInformation(message: "{Result}", "Success youtube prompt");
        return Result.Success();
    }
}