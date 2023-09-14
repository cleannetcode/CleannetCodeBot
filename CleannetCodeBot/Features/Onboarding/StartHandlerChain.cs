using CleannetCodeBot.Core;
using CleannetCodeBot.Infrastructure;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CleannetCodeBot.Features.Onboarding;

public class PrivateStartHandlerChain : IHandlerChain
{
    private readonly ILogger<PrivateStartHandlerChain> _logger;
    private readonly IOnboardingBotClient _onboardingBotClient;

    private readonly IMongoCollection<Member> _membersCollection;

    public PrivateStartHandlerChain(
        IOnboardingBotClient onboardingBotClient,
        IMongoDatabase mongoDatabase,
        ILogger<PrivateStartHandlerChain> logger)
    {
        _onboardingBotClient = onboardingBotClient;
        _membersCollection = mongoDatabase.GetCollection<Member>(Member.CollectionName);
        _logger = logger;
    }

    public int OrderInChain => 0;

    public async Task<Result> HandleAsync(TelegramRequest request, CancellationToken cancellationToken = default)
    {
        var isPrivateChat = request.IsPrivateChat();
        if (isPrivateChat.IsFailure || request.Update is not { Message.From: { } })
        {
            return isPrivateChat;
        }

        var message = request.Update.Message;
        var text = message?.Text ?? string.Empty;
        if (message == null || text != OnboardingBotCommands.StartCommand)
        {
            return HandlerResults.NotMatchingType;
        }

        if (message.From == null)
        {
            return Result.Failure("Message.From is null. This should not happen in order to process user info");
        }

        var userId = message.From.Id;
        var member = await _membersCollection
            .Find(x => x.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);
        if (member is not null && member.Started)
        {
            return Errors.NotMatchingStateResult();
        }

        member = new Member(
            id: message.From.Id,
            username: message.From.Username,
            firstName: message.From.FirstName,
            lastName: message.From.LastName ?? "",
            started: true,
            personalChatId: request.Update.Message.Chat.Id
        );

        await _membersCollection.InsertOneAsync(member, cancellationToken: cancellationToken);
        await _onboardingBotClient.SendOnboardingMessageAsync(
            userId: member.Id,
            username: member.Username,
            chatId: member.PersonalChatId!.Value,
            cancellationToken: cancellationToken);

        _logger.LogInformation(message: "{Result}", "Success welcome message in personal chat sent");
        return Result.Success();
    }
}