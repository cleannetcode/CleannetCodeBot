using CleannetCode_bot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace CleannetCode_bot.Features.Welcome.HandlerChains;

public class WelcomeYoutubeAnswerHandlerChain : WelcomePrivateHandlerChain
{
    private readonly ILogger<WelcomeYoutubeAnswerHandlerChain> _logger;

    public WelcomeYoutubeAnswerHandlerChain(
        IWelcomeBotClient welcomeBotClient,
        IGenericRepository<long, WelcomeUserInfo> welcomeUserInfoRepository,
        ILogger<WelcomeYoutubeAnswerHandlerChain> logger) : base(
        welcomeBotClient: welcomeBotClient,
        welcomeUserInfoRepository: welcomeUserInfoRepository)
    {
        _logger = logger;
    }

    protected override WelcomeUserInfoState TargetState => WelcomeUserInfoState.AskingYoutube;

    protected async override Task<Result> ProcessUserAsync(
        long userId,
        WelcomeUserInfo user,
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
        await WelcomeBotClient.SendYoutubeConfirmedAsync(
            chatId: user.PersonalChatId!.Value,
            cancellationToken: cancellationToken);
        _logger.LogInformation(message: "{Result}", "Success youtube answer handling");
        return Result.Success();
    }
}