using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using System.Linq;
using Microsoft.Extensions.Options;

namespace CleannetCode_bot.Features.Homework;

public class HomeworkHandler
{
    private readonly ITelegramBotClient _client;
    private readonly HomeworkServiceOptions _config;
    private readonly ILogger<HomeworkHandler> _logger;
    private readonly GithubRepository _githubRepository;
    private readonly HomeworkStorageRepository<CommentsDiscussions> _storageCommentsDiscussion;
    private readonly HomeworkStorageRepository<HomeworkOnRepositories> _storageHomeworkOnRepositories;

    public HomeworkHandler(ITelegramBotClient client, IOptions<HomeworkServiceOptions> config, ILogger<HomeworkHandler> logger)
    {
        _client = client;
        _config = config.Value;
        _logger = logger;

        // var isValidToken = Task.Run(() => GithubRepository.IsValidToken(_config.GithubToken)).Result;
        // if (!isValidToken)
        // {
        //     _logger.LogError("invalid GithubToken");
        //     throw new Exception("invalid GithubToken");
        // }

        _githubRepository = new GithubRepository(_config.GithubToken);

        _storageCommentsDiscussion = new HomeworkStorageRepository<CommentsDiscussions>(
            _config.FileNameStorageCommentsDiscussions
        );
        _storageHomeworkOnRepositories = new HomeworkStorageRepository<HomeworkOnRepositories>(
            _config.FileNameStorageHomeworkOnRepositories
        );
    }

    public async Task Start()
    {
        var newDiscussionsComments = await GetNewDiscussionsComments();
        await SendNewMessages(newDiscussionsComments);

        var updatedHomeworkOnRepositories = await GetUpdatedHomeworkOnRepositories();
        await SendRepositoriesUpdates(updatedHomeworkOnRepositories);
    }

    private static readonly string[] _avatars = new string[] {
        "🐰", "🦊", "🐻", "🐼", "🐨", "🦁", "🐸", "🐝",
        "🐶", "🐱", "🐭", "🐹", "🐰", "🐻", "🐼", "🐨",
        "🦁", "🐯", "🐮", "🐷", "🐸", "🐙", "🦑", "🐟",
        "🐠", "🐡", "🦀", "🦐", "🦔", "🦝", "🦦", "🦢"
    };

    private async Task SendNewMessages(CommentsDiscussions newDiscussionsComments)
    {
        foreach (var (url, comments) in newDiscussionsComments.Discussions)
        {
            string pattern = @"/(\d+)$";
            var idDiscussion = Regex.Match(url, pattern).Groups[1].Value;

            foreach (var comment in comments)
            {
                var hyperlinkUrlDiscussion = $"[#{idDiscussion}]({url}#discussioncomment-{comment.CommentId})";
                var sendMessage = $"🗒 {hyperlinkUrlDiscussion}" +
                    $"  {_avatars[new Random().Next(_avatars.Length)]}" +
                    $" [{comment.Author}](https://github.com/{comment.Author})" +
                    $" on {comment.DateCreate:MMM dd, yyyy}" +
                    $"\n\n{comment.Message}";

                var clearTextMessage = GetClearText(sendMessage);

                await _client.SendTextMessageAsync(
                    _config.TgChannelID,
                    clearTextMessage,
                    _config.TgThreadIdForNewComments,
                    Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    disableWebPagePreview: true,
                    disableNotification: true
                );

                await Task.Delay(10000);
            }
        }
    }

    private async Task SendRepositoriesUpdates(HomeworkOnRepositories updatedHomeworkOnRepositories)
    {
        foreach (var repositoryInfo in updatedHomeworkOnRepositories.Repositories)
        {
            string pattern = @"^https:\/\/github.com\/(?<author>[^\/]+)\/.*$";
            var match = Regex.Match(repositoryInfo.Link, pattern);
            if (!match.Success)
                continue;

            var author = match.Groups["author"].Value;

            var kekw999lvlDesign = repositoryInfo.CountStars > 0 ? "★" : "☆";
            var sendMessage = $"🕵️ {kekw999lvlDesign}{repositoryInfo.CountStars}" +
                $"  {_avatars[new Random().Next(_avatars.Length)]}" +
                $" [{author}](https://github.com/{author})" +
                $" on {repositoryInfo.LastUpdate:MMM dd, yyyy}" +
                $"\n\ncommit: [{repositoryInfo.LastCommitMessage}]({repositoryInfo.CommitLink})";

            await _client.SendTextMessageAsync(
                _config.TgChannelID,
                GetClearText(sendMessage),
                _config.TgThreadIdForNewRepoUpdates,
                Telegram.Bot.Types.Enums.ParseMode.Markdown,
                disableWebPagePreview: true,
                disableNotification: true
            );
            await Task.Delay(10000);
        }
    }

    private static string GetClearText(string dirtyText)
    {
        string pattern = @"[_*~`]";
        return Regex.Replace(dirtyText, pattern, @"\$&");

        // string pattern = @"(?:https://[^\s\\)""<>]+)|([_*~]{1,3})";
        // string replacement = @""; // = @"\$&"; 

        // return Regex.Replace(dirtyText, pattern, match =>
        //     match.Groups[1].Success ? Regex.Replace(match.Value, pattern, replacement) : match.Value
        // );
    }

    private async Task<CommentsDiscussions> GetNewDiscussionsComments()
    {
        var newCommentsDiscussions = new CommentsDiscussions();

        var savedCommentsDiscussions = await _storageCommentsDiscussion.Get();
        if (savedCommentsDiscussions?.Discussions == null)
            return newCommentsDiscussions;

        var currentCommentsDiscussions = await _githubRepository.GetCommentsDiscussions(
            _config.Organization,
            _config.Repositories

        );
        if (currentCommentsDiscussions?.Discussions == null)
            return newCommentsDiscussions;

        var newSavedCommentsDiscussions = savedCommentsDiscussions;
        var isNeedSave = false;

        foreach (var (url, currentComments) in currentCommentsDiscussions.Discussions)
        {
            var isFirstStart = !savedCommentsDiscussions.Discussions.TryGetValue(url, out var savedComments);
            if (isFirstStart || savedComments == null)
            {
                newSavedCommentsDiscussions.Discussions.Add(url, currentComments);
                isNeedSave = true;

                await AddHomeworkOnRepositories(currentComments);

                // Warning!!!: Spamming on first start!!!
                newCommentsDiscussions.Discussions.Add(url, currentComments);
            }
            else
            {
                /*
                saved =   [A, B, C, D]
                current = [A,    C*, D, E, F]

                new = current - saved = [C*, E, F]
                changed = saved - current = [B, C]

                result = [A, B_, C*, D, E, F]
                */

                var newComments = currentComments.Except(savedComments).ToList();

                if (newComments.Count == 0)
                    continue;

                var changedComments = savedComments.Except(currentComments);

                // [C*, E, F] in [B, C] return [C*]
                var editedComments = newComments
                    .Where(comment => changedComments.Any(changedComment =>
                        changedComment.DateCreate == comment.DateCreate))
                    .ToList();

                // [B, C] in [C*, E, F] return [B]
                var deletedComments = changedComments
                    .Where(changedComment => newComments.All(comment =>
                        comment.DateCreate != changedComment.DateCreate))
                    .ToList();

                // [A, B, C, D] .replace ( [C], [C*] ) = [A, B, C*, D]
                var newSavedComments = savedComments
                    .Select(comment => editedComments.FirstOrDefault(editedComment =>
                        editedComment.DateCreate == comment.DateCreate) ?? comment)
                    .ToList();

                // [C*, E, F] - [C*] = [E, F]
                newComments.RemoveAll(comment => editedComments.Contains(comment));

                if (newComments.Count > 0)
                {
                    newCommentsDiscussions.Discussions.Add(url, newComments.ToArray());
                    await AddHomeworkOnRepositories(newComments.ToArray());
                }

                // [A, B, C*, D] + [E, F] = [A, B, C*, D, E, F]
                newSavedComments.AddRange(newComments);

                if (newSavedComments.Count > 0)
                {
                    newSavedCommentsDiscussions.Discussions[url] = newSavedComments.ToArray();
                    isNeedSave = true;
                }
            }
        }

        if (isNeedSave)
        {
            var isSaved = await _storageCommentsDiscussion.Save(newSavedCommentsDiscussions);
            if (!isSaved)
                _logger.LogCritical("New comments discussions are not saved to storage");
        }

        return newCommentsDiscussions;
    }

    private async Task<HomeworkOnRepositories> GetUpdatedHomeworkOnRepositories()
    {
        var result = new HomeworkOnRepositories();

        var savedRepositories = await _storageHomeworkOnRepositories.Get();
        if (savedRepositories?.Repositories == null)
            return result;

        var linksToRepositories = savedRepositories.Repositories
            .Select(v => v.Link)
            .ToArray();

        var currentRepositories = await _githubRepository.GetCurrentHomeworkOnRepositories(linksToRepositories);
        if (currentRepositories?.Repositories == null)
            return result;

        var newSavedRepositories = savedRepositories;

        /*
        saved =   [A, B, C, D]
        current = [A,    C*, D]

        new = current - saved = [C*]
        changed = saved - current = [B, C]

        result = [A, C*, D]
        */

        var newRepositories = currentRepositories.Repositories
            .Except(savedRepositories.Repositories)
            .ToList();

        if (newRepositories.Count == 0)
            return result;

        var changedRepositories = savedRepositories.Repositories
            .Except(currentRepositories.Repositories)
            .ToList();


        // [C*] in [B, C] return [C*]
        var editedRepositories = newRepositories
            .Where(newRepo => changedRepositories.Any(changed =>
                changed.Link == newRepo.Link))
            .ToList();

        // [B, C] in [C*] return [B]
        var deletedRepositories = changedRepositories
            .Where(changed => newRepositories.All(newRepo =>
                newRepo.Link != changed.Link))
            .ToList();

        // [A, B, C, D] .replace ( [C], [C*] ) = [A, B, C*, D]
        newSavedRepositories.Repositories = newSavedRepositories.Repositories
            .Select(repository => editedRepositories.FirstOrDefault(edited =>
                edited.Link == repository.Link) ?? repository)
            .ToList();

        // [A, B, C*, D] .replace ( [B], [] ) = [A, C*, D]
        newSavedRepositories.Repositories.RemoveAll(repository => deletedRepositories.Contains(repository));

        if (newRepositories.Count > 0)
        {
            result.Repositories.AddRange(newRepositories.ToArray());
            await _storageHomeworkOnRepositories.Save(newSavedRepositories);
        }

        return result;
    }

    private async Task AddHomeworkOnRepositories(Comment[] currentComments)
    {
        if (currentComments == null || currentComments.Length == 0)
            return;

        var savedHomeworkOnRepositories = await _storageHomeworkOnRepositories.Get();

        var linksToRepositories = GetRepositoriesInfoFromComments(currentComments);
        var currentHomeworkOnRepositories = await _githubRepository.GetCurrentHomeworkOnRepositories(linksToRepositories);

        var newHomeworkOnRepositories = currentHomeworkOnRepositories.Repositories.Except(savedHomeworkOnRepositories.Repositories);
        savedHomeworkOnRepositories.Repositories.AddRange(newHomeworkOnRepositories);

        await _storageHomeworkOnRepositories.Save(savedHomeworkOnRepositories);
    }

    private static string[] GetRepositoriesInfoFromComments(Comment[] comments)
    {
        var result = new List<string>();

        var pattern = @"https://github\.com/[A-Za-z0-9-_]+/[A-Za-z0-9-_]+";
        var regex = new Regex(pattern);

        foreach (var comment in comments)
        {
            var matches = regex.Matches(comment.Message);
            result.AddRange(matches.Select(match => match.Value));
        }

        return result.ToArray();
    }
}