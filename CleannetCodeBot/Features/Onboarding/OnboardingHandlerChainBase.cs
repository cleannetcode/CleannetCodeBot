using CleannetCodeBot.Core;
using CleannetCodeBot.Infrastructure;
using CSharpFunctionalExtensions;
using MongoDB.Driver;

namespace CleannetCodeBot.Features.Onboarding;

public abstract class OnboardingHandlerChainBase : IHandlerChain
{
    protected readonly IOnboardingBotClient OnboardingBotClient;
    protected readonly IMongoCollection<Member> MembersCollection;

    protected OnboardingHandlerChainBase(
        IOnboardingBotClient onboardingBotClient,
        IMongoDatabase mongoDatabase)
    {
        OnboardingBotClient = onboardingBotClient;
        MembersCollection = mongoDatabase.GetCollection<Member>(Member.CollectionName);
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
        
        var user = await MembersCollection
            .Find(x => x.Id == userId)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
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