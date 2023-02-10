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

    public HomeworksHandler(ITelegramBotClient client, HomeworksServiceOptions config, ILogger<HomeworksHandler> logger)
    {
        this._client = client;
        this._config = config;
        this._logger = logger;
    }

    public async void Start()
    {
        var arrNewMessages = await GetNewMessages();
        SendNewMessages(arrNewMessages);
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

    public async Task<DiscussionMessages[]> GetNewMessages()
    {
        var discussionMessagesRepository = new DiscussionMessagesRepository(_config.FileNameCache ?? "cachedHomeworkMessages0.json");

        var tasks = new List<Task<DiscussionMessages[]>>();

        foreach (var organization in _config.Organizations ?? Array.Empty<Organization>())
        {
            foreach (var repository in organization.Repositories ?? Array.Empty<Repository>())
            {
                foreach (var discussionID in repository.DiscussionsID ?? Array.Empty<int>())
                {
                    tasks.Add(discussionMessagesRepository.UpdateCacheAndGetNewMessages(
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