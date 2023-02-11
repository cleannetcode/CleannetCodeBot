using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Microsoft.VisualBasic;

namespace CleannetCode_bot.Features.Homeworks;

public class HomeworksHandler
{
    private readonly ITelegramBotClient _client;
    private readonly HomeworksServiceOptions _config;
    private readonly ILogger<HomeworksHandler> _logger;
    private readonly GithubDataRepository _discussionMessagesRepository;

    public HomeworksHandler(ITelegramBotClient client, HomeworksServiceOptions config, ILogger<HomeworksHandler> logger)
    {
        this._client = client;
        this._config = config;
        this._logger = logger;

        this._discussionMessagesRepository = new GithubDataRepository(_config.FileNameCache ?? "cachedHomeworkMessages0.json");
    }

    public async void Start()
    {
        var arrNewMessages = await GetNewMessages();
        SendNewMessages(arrNewMessages);

        var arrUpdatesLinks = await GetUpdatedLinks();
        SendUpdatesLinks(arrUpdatesLinks);
    }

    public async void SendUpdatesLinks(LinkData[] arrUpdatesLinks)
    {
        foreach (var item in arrUpdatesLinks)
        {
            await _client.SendTextMessageAsync(
                _config.TelegramChannelID ?? "",
                $"Link: {item.Link}\n\n Commit:{item.LastCommit}");
        }
    }
    public async void SendNewMessages(DiscussionMessages[] arrNewMessages)
    {
        foreach (var messagePage in arrNewMessages)
        {
            await _client.SendTextMessageAsync(
                _config.TelegramChannelID ?? "",
                $"Author: {messagePage.Author}\n\n {messagePage.Message}"); // messagePage.DatetimeCreateNode
        }
    }

    // todo: спасть уже хочу :D Сделать с такими же параметрами как UpdateCacheAndGetNewMessages 
    // и добавить tasks (а то долго) где находить обновленные репы через .Except
    public async Task<LinkData[]> GetUpdatedLinks()
    {
        var arrUpdatesLinks = new List<LinkData>();

        // <> todo: 
        //
        // var tasks = new List<Task<LinkData>>();

        var cache = _discussionMessagesRepository.Get();
        var newCache = new HomeworksCache() { DiscussionsData = new Dictionary<string, DiscussionData>() };

        foreach (var item in cache.DiscussionsData ?? new Dictionary<string, DiscussionData>())
        {
            var newLinksItems = new List<LinkData>();

            foreach (var linkData in item.Value.Links ?? new List<LinkData>())
            {
                var newlinkData = await _discussionMessagesRepository.GetLastUpdateLinkData(linkData);
                if (newlinkData.LastGithubUpdate != linkData.LastGithubUpdate)
                    arrUpdatesLinks.Add(newlinkData);

                newLinksItems.Add(newlinkData);
            }

            var discussionData = new DiscussionData()
            {
                Messages = item.Value.Messages,
                Links = item.Value.Links
            };

            if (arrUpdatesLinks.Count > 0)
            {
                discussionData.Links = newLinksItems;
            }

            newCache.DiscussionsData?.Add(item.Key, discussionData);
        }

        if (cache != newCache)
            _discussionMessagesRepository.Save(cache);

        // <> todo: 
        //
        // foreach (var task in tasks)
        // {
        //     try
        //     {
        //         arrUpdatesLinks.Add(await task);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "An error occurred while executing the task of retrieving github links with homework.");
        //     }
        // }

        return arrUpdatesLinks.ToArray();
    }

    public async Task<DiscussionMessages[]> GetNewMessages()
    {
        var tasks = new List<Task<DiscussionMessages[]>>();

        foreach (var organization in _config.Organizations ?? Array.Empty<Organization>())
        {
            foreach (var repository in organization.Repositories ?? Array.Empty<Repository>())
            {
                foreach (var discussionID in repository.DiscussionsID ?? Array.Empty<int>())
                {
                    tasks.Add(_discussionMessagesRepository.UpdateCacheAndGetNewMessages(
                        organization.OrganizationName ?? "",
                        repository.RepositoryName ?? "",
                        discussionID));
                }
            }
        }

        var arrNewMessage = new List<DiscussionMessages>();

        foreach (var task in tasks)
        {
            try
            {
                arrNewMessage.AddRange(await task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing the task of retrieving messages with homework.");
            }
        }

        // var resultAllTasks = await Task.WhenAll(tasks);
        // var arrNewMessage = resultAllTasks.Cast<DiscussionMessages>().ToArray();

        return arrNewMessage.ToArray();
    }
}