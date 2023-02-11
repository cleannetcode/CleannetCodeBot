// todo <> section in config

using HtmlAgilityPack;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace CleannetCode_bot.Features.Homeworks;

public class DiscussionMessagesRepository
{
    private string _fileName;
    public static string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;

    public DiscussionMessagesRepository(string fileName)
    {
        this._fileName = fileName;
    }

    public async Task<DiscussionMessages[]> GetMessagesFromDiscussion(string organizationName, string repositoryName, int discussionID)
    {
        var url = $"https://github.com/{organizationName}/{repositoryName}/discussions/{discussionID}";

        var client = new HttpClient();
        var response = await client.GetAsync(url);
        var httpPage = await response.Content.ReadAsStringAsync();

        var discussionMessages = new List<DiscussionMessages>();

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(httpPage);

        var discussionPosts = htmlDoc.DocumentNode.SelectNodes("//div[@class=\"js-timeline-item js-timeline-progressive-focus-container\"]");
        if (discussionPosts != null)
        {
            foreach (var discussionPost in discussionPosts)
            {
                var authorNameNode = discussionPost.SelectSingleNode(".//span[@class=\"Truncate-text text-bold\"]");
                var messageNode = discussionPost.SelectSingleNode(".//td[@class=\"d-block color-fg-default comment-body markdown-body js-comment-body\"]");
                var timestampCreateNode = discussionPost.SelectSingleNode(".//relative-time[not(@datetime='{{datetime}}')]")
                    .GetAttributeValue("datetime", null);
                // maybe todo: 
                // var reactionsNode = ...;
                // var lastEditedTimestampNode = ...;
                // var commentsNode = ...;

                var datetimeCreateNode = DateTimeOffset.Parse(timestampCreateNode);

                if (authorNameNode != null && messageNode != null)
                {
                    discussionMessages.Add(new DiscussionMessages(
                        authorNameNode.InnerText,
                        messageNode.InnerText,
                        datetimeCreateNode));
                }
            }
        }

        return discussionMessages.ToArray();
    }

    public DiscussionMessages[] UpdateCacheAndGetNewMessages(string organizationName, string repositoryName, int discussionID, DiscussionMessages[] messages)
    {
        var newMessages = new List<DiscussionMessages>();
        var homeworksCache = Get();

        var url = $"https://github.com/{organizationName}/{repositoryName}/discussions/{discussionID}";
        var discussionData = new DiscussionData() { };

        var isGetDiscussionData = homeworksCache.DiscussionsData?.TryGetValue(url, out discussionData) ?? false;

        if (isGetDiscussionData)
        {
            var exceptedMessages = discussionData?.Messages?.Except(messages) ?? new List<DiscussionMessages>();
            newMessages.AddRange(exceptedMessages.Where(message => messages.Contains(message)));
        }

        discussionData = new DiscussionData() { Messages = messages.ToList() };
        if (homeworksCache.DiscussionsData?.ContainsKey(url) ?? false)
        {
            homeworksCache.DiscussionsData[url] = discussionData;
        }
        else
        {
            homeworksCache.DiscussionsData?.Add(url, discussionData);
        }

        Save(homeworksCache);

        return newMessages.ToArray();
    }

    private bool Save(HomeworksCache cache)
    {
        var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
        });

        var path = Path.Combine(BaseDirectory, _fileName);
        File.WriteAllText(path, json);

        return true;
    }

    private HomeworksCache Get()
    {
        var homeworksCache = new HomeworksCache();
        var path = Path.Combine(BaseDirectory, _fileName);

        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            homeworksCache = JsonSerializer.Deserialize<HomeworksCache>(json) ??
                new HomeworksCache() { DiscussionsData = new Dictionary<string, DiscussionData>() };
        }
        else
        {
            homeworksCache.DiscussionsData = new Dictionary<string, DiscussionData>();
        }

        return homeworksCache;
    }
}


