using CleannetCodeBot.Core;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CleannetCodeBot.Features.Onboarding.HandlerChains;

public class GetUserInfoHandlerChain : OnboardingHandlerChainBase
{
    private readonly ILogger<GetUserInfoHandlerChain> _logger;

    public GetUserInfoHandlerChain(
        IOnboardingBotClient onboardingBotClient,
        IMongoDatabase mongoDatabase,
        ILogger<GetUserInfoHandlerChain> logger) : base(
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
        if (text != OnboardingBotCommands.GetMyInfoCommand)
            return Errors.NotMatchingStateResult();
        await OnboardingBotClient.SendInformationAsync(
            chatId: user.PersonalChatId!.Value,
            username: user.Username,
            githubNick: user.GithubNick,
            youtubeName: user.YoutubeName,
            cancellationToken: cancellationToken);
        _logger.LogInformation(message: "{Result}", "Success return information");
        return Result.Success();
    }
}