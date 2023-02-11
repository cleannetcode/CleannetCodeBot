namespace CleannetCode_bot.Features.Welcome;

public interface IWelcomeBotClient
{
    Task SendWelcomeMessageInCommonChatAsync(
        string username,
        long userId,
        long chatId,
        CancellationToken cancellationToken = default);

    Task SendYoutubePromptAsync(
        long chatId,
        CancellationToken cancellationToken = default);

    Task SendYoutubeConfirmedAsync(
        long chatId,
        CancellationToken cancellationToken = default);

    Task SendGithubPromptAsync(
        long chatId,
        CancellationToken cancellationToken = default);

    Task SendGithubConfirmedAsync(
        long chatId,
        CancellationToken cancellationToken = default);

    Task SendGithubInvalidProfileAsync(
        long chatId,
        CancellationToken cancellationToken = default);

    Task SendInformationAsync(
        long chatId,
        string? username,
        string? githubNick,
        string? youtubeName,
        CancellationToken cancellationToken = default);

    Task SendInformationRemovedSuccessfulAsync(
        long chatId,
        CancellationToken cancellationToken = default);

    Task SendWelcomeMessageInPersonalChatAsync(
        string username,
        long userId,
        long chatId,
        CancellationToken cancellationToken = default);
}