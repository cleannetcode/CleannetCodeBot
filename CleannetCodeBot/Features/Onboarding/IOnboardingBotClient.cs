namespace CleannetCodeBot.Features.Onboarding;

public interface IOnboardingBotClient
{
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

    Task SendOnboardingMessageAsync(
        string username,
        long userId,
        long chatId,
        CancellationToken cancellationToken = default);
}