using CleannetCode_bot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace CleannetCode_bot.Features.Welcome.HandlerChains;

public class WelcomeYoutubePromptHandlerChain : WelcomePrivateHandlerChain
{
    private readonly ILogger<WelcomeYoutubePromptHandlerChain> _logger;

    public WelcomeYoutubePromptHandlerChain(
        IWelcomeBotClient welcomeBotClient,
        IGenericRepository<long, WelcomeUserInfo> welcomeUserInfoRepository,
        ILogger<WelcomeYoutubePromptHandlerChain> logger) : base(
        welcomeBotClient: welcomeBotClient,
        welcomeUserInfoRepository: welcomeUserInfoRepository)
    {
        _logger = logger;
    }

    protected override WelcomeUserInfoState TargetState => WelcomeUserInfoState.Idle;

    protected async override Task<Result> ProcessUserAsync(
        long userId,
        WelcomeUserInfo user,
        string text,
        CancellationToken cancellationToken)
    {
        if (text != WelcomeBotCommandNames.ChangeYoutubeInfoCommand)
            return WelcomeHandlerHelpers.NotMatchingStateResult;

        await WelcomeUserInfoRepository.SaveAsync(
            key: userId,
            entity: user with
            {
                State = WelcomeUserInfoState.AskingYoutube
            },
            cancellationToken: cancellationToken);
        await WelcomeBotClient.SendYoutubePromptAsync(
            chatId: user.PersonalChatId!.Value,
            cancellationToken: cancellationToken);
        // TODO: Сделать проверку на существование профиля в Github
        _logger.LogInformation(message: "{Result}", "Success youtube prompt");
        return Result.Success();
    }
}