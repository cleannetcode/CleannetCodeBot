using CleannetCodeBot.Core;
using CleannetCodeBot.Infrastructure;
using CleannetCodeBot.Infrastructure.DataAccess.Interfaces;
using CSharpFunctionalExtensions;

namespace CleannetCodeBot.Features.Onboarding;

public abstract class OnboardingHandlerChainBase : IHandlerChain
{
    protected readonly IOnboardingBotClient OnboardingBotClient;
    protected readonly IGenericRepository<long, Member> WelcomeUserInfoRepository;

    protected OnboardingHandlerChainBase(
        IOnboardingBotClient onboardingBotClient,
        IGenericRepository<long, Member> welcomeUserInfoRepository)
    {
        OnboardingBotClient = onboardingBotClient;
        WelcomeUserInfoRepository = welcomeUserInfoRepository;
    }

    protected abstract WelcomeUserInfoState TargetState { get; }

    public int OrderInChain => 0;

    public async Task<Result> HandleAsync(TelegramRequest request, CancellationToken cancellationToken = default)
    {
        var isPrivateChat = request.IsPrivateChat();
        if (isPrivateChat.IsFailure)
        {
            return isPrivateChat;
        }

        var userId = request.Update.Message?.From?.Id ?? default;
        var user = await WelcomeUserInfoRepository.ReadAsync(
            key: userId,
            cancellationToken: cancellationToken);
        if (user is null || user.State != TargetState)
        {
            return Errors.NotMatchingStateResult();
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

    protected abstract Task<Result> ProcessUserAsync(long userId, Member user, string text, CancellationToken cancellationToken);
}