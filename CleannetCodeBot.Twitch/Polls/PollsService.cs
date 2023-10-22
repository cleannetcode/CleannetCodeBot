using System.Text.Json;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward;
using TwitchLib.Api.Interfaces;

namespace CleannetCodeBot.Twitch.Polls;

public class PollsService : IPollsService
{
    private readonly IPollsRepository _pollsRepository;
    private readonly IQuestionsRepository _questionsRepository;
    private readonly IUsersPollStartRegistry _usersPollStartRegistry;
    private readonly ITwitchAPI _twitchApi;
    private readonly ILogger<PollsService> _logger;
    private readonly IOptions<PollSettings> _pollSettings;
    private readonly IMongoCollection<Vote> _votesCollection;

    public PollsService(IPollsRepository pollsRepository, 
        IQuestionsRepository questionsRepository,
        IUsersPollStartRegistry usersPollStartRegistry,
        ITwitchAPI twitchApi, 
        ILogger<PollsService> logger, 
        IOptions<PollSettings> pollSettings,
        IMongoDatabase mongoDatabase)
    {
        _pollsRepository = pollsRepository;
        _questionsRepository = questionsRepository;
        _usersPollStartRegistry = usersPollStartRegistry;
        _twitchApi = twitchApi;
        _logger = logger;
        _pollSettings = pollSettings;
        
        _votesCollection =  mongoDatabase.GetCollection<Vote>(Vote.CollectionName);;
    }

    public async Task CreatePoll(string userId, string username, string broadCasterId, string authToken)
    {
        _logger.LogInformation($"Trying create poll by user {userId}");
        if (!_usersPollStartRegistry.AddUserTryRecord(userId, DateTime.UtcNow.AddHours(_pollSettings.Value.UserPollCreationGapInHours))) 
        {
            _logger.LogInformation($"Cannot create poll by user {userId} request. Time gap not expired");
            return;
        }

        var question = _questionsRepository.GetRandomQuestion();
        _logger.LogInformation($"Received question for poll creating by user {userId}");
        
        // Create reward
        var requestPayload = new CreateCustomRewardsRequest()
        {
            Title = $"Вопрос от {username}",
            Cost = 1,
            Prompt = question.ToString(),
            IsUserInputRequired = true,
            IsEnabled = true
        };
        
        
        string rewardId;
        try
        {
            await RemoveRewardWithTitle(requestPayload.Title, broadCasterId, authToken);

            _logger.LogInformation($"Trying create reward for poll creating by user {userId}");
            var requestResult = await _twitchApi.Helix.ChannelPoints.CreateCustomRewardsAsync(broadCasterId, requestPayload, authToken);
            rewardId = requestResult.Data.First().Id;
            _logger.LogInformation($"Reward created for poll. RewardId {rewardId}");
        }
        catch (Exception e)
        {
            _usersPollStartRegistry.RemoveUserTryRecord(userId);
            _logger.LogError($"Error occured on creating custom poll reward: {e.Message}");
            return;
        }
        
        // Create poll with id
        var poll = new Poll(question, broadCasterId, rewardId, userId, DateTime.UtcNow.AddMinutes(_pollSettings.Value.PollDurationInMinutes));
        
        _pollsRepository.AddPoll(poll);
        _logger.LogInformation($"Poll created by user {userId}");
    }

    public async Task ClosePoll(string pollRewardId, string broadcasterId, string authToken)
    {
        _logger.LogInformation($"Trying close poll with reward id {pollRewardId}");
        var poll = _pollsRepository.GetPollByRewardId(pollRewardId);
        if (poll is null)
        {
            _logger.LogInformation($"Poll with reward id {pollRewardId} not found");
            return;
        }

        _pollsRepository.RemovePollByRewardId(pollRewardId);

        // two operations can be done in parallel:
        // Remove poll reward
        _logger.LogInformation($"Trying delete poll reward with id {pollRewardId}");
        try
        {
            await _twitchApi.Helix.ChannelPoints.DeleteCustomRewardAsync(broadcasterId, poll.RewardId, authToken);
            _logger.LogInformation($"Poll reward with id {poll.RewardId} removed");
        }
        catch (Exception e)
        {
            _logger.LogError($"Error occured on deleting custom poll reward: {e.Message}");
            // retry?
        }
        
        await _votesCollection.InsertManyAsync(poll.Results());
    }

    public void AddVoteToPoll(string pollRewardId, string userId, string answer)
    {
        _logger.LogInformation($"Trying to vote with reward {pollRewardId} by user {userId}");
        var poll = _pollsRepository.GetPollByRewardId(pollRewardId);
        
        poll?.AddVote(userId, answer);
    }

    private async Task RemoveRewardWithTitle(string title, string broadCasterId, string authToken)
    {
        _logger.LogInformation($"Trying to find and remove reward with title {title}");
        try
        {
            _logger.LogInformation("Get reward list");
            var rewards = await _twitchApi.Helix.ChannelPoints.GetCustomRewardAsync(broadcasterId: broadCasterId, onlyManageableRewards: true,
                accessToken: authToken);
            
            _logger.LogInformation($"Received {rewards.Data.Length} rewards");

            var rewardExists = rewards.Data.FirstOrDefault(x => x.Title == title);
            if (rewardExists is not null)
            {
                _logger.LogInformation($"Found reward with requested title {title}. Trying to delete");
                await _twitchApi.Helix.ChannelPoints.DeleteCustomRewardAsync(broadCasterId, rewardExists.Id, authToken);
                _logger.LogInformation($"Reward with title {title} was deleted");
            }
            else
            {
                _logger.LogInformation($"Reward with requested title {title} not found");
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Error occured on finding and deleting reward with title {title}. Details {e.Message}");
            throw;
        }
    }
}