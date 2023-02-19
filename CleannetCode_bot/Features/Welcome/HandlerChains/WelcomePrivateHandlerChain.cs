using CleannetCode_bot.Infrastructure;
using CleannetCode_bot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;

namespace CleannetCode_bot.Features.Welcome.HandlerChains;

[IgnoreAutoInjection]
public abstract class WelcomePrivateHandlerChain : IHandlerChain
{
    protected readonly IWelcomeBotClient WelcomeBotClient;
    protected readonly IGenericRepository<long, WelcomeUserInfo> WelcomeUserInfoRepository;

    protected WelcomePrivateHandlerChain(
        IWelcomeBotClient welcomeBotClient,
        IGenericRepository<long, WelcomeUserInfo> welcomeUserInfoRepository)
    {
        WelcomeBotClient = welcomeBotClient;
        WelcomeUserInfoRepository = welcomeUserInfoRepository;
    }

    protected abstract WelcomeUserInfoState TargetState { get; }

    public int OrderInChain => 0;

    public async Task<Result> HandleAsync(TelegramRequest request, CancellationToken cancellationToken = default)
    {
        var privateCheck = request.IsPrivateChat();
        if (privateCheck.IsFailure)
        {
            return privateCheck;
        }

        var userId = request.Update.Message?.From?.Id ?? default;
        var user = await WelcomeUserInfoRepository.ReadAsync(
            key: userId,
            cancellationToken: cancellationToken);
        if (user is null || user.State != TargetState)
        {
            return WelcomeHandlerHelpers.NotMatchingStateResult;
        }
        user = user with
        {
            PersonalChatId = request.Update.Message!.Chat.Id
        };

        var text = request.Update.Message?.Text ?? string.Empty;
        return await ProcessUserAsync(
            userId: userId,
            user: user,
            text: text,
            cancellationToken: cancellationToken);
    }

    protected abstract Task<Result> ProcessUserAsync(long userId, WelcomeUserInfo user, string text, CancellationToken cancellationToken);
}