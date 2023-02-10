// todo <> section in config

using HtmlAgilityPack;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Serilog.Debugging;
using System.Text.RegularExpressions;

namespace CleannetCode_bot.Features.Homeworks;

public class GithubDataRepository
{
    private string _fileName;
    public static string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;

    public GithubDataRepository(string fileName)
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

    public async Task<DiscussionMessages[]> UpdateCacheAndGetNewMessages(string organizationName, string repositoryName, int discussionID)
    {
        var messages = await GetMessagesFromDiscussion(
            organizationName,
            repositoryName,
            discussionID);

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
        else
        {
            var linksData = await GetLinksData(messages);
            discussionData = new DiscussionData()
            {
                Messages = messages.ToList(),
                Links = linksData.ToList() // <>
            };

            if (homeworksCache.DiscussionsData?.ContainsKey(url) ?? false)
            {
                homeworksCache.DiscussionsData[url] = discussionData;
            }
            else
            {
                homeworksCache.DiscussionsData?.Add(url, discussionData);
            }

            Save(homeworksCache);
        }

        if (newMessages.Count > 0)
        {
            homeworksCache.DiscussionsData?[url]?.Messages?.AddRange(newMessages);

            var linksData = await GetLinksData(newMessages.ToArray());
            homeworksCache.DiscussionsData?[url]?.Links?.AddRange(linksData);

            Save(homeworksCache);
        }

        return newMessages.ToArray();
    }

    public async Task<LinkData[]> GetLinksData(DiscussionMessages[] messages)
    {
        var linksData = new List<LinkData>();

        var pattern = @"https://github\.com/[A-Za-z0-9-_]+/[A-Za-z0-9-_]+";
        var regex = new Regex(pattern);

        foreach (var messagePage in messages)
        {
            var matches = regex.Matches(messagePage.Message);
            foreach (Match match in matches)
            {
                var lastUpdateLinkData = await GetLastUpdateLinkData(new LinkData
                {
                    Link = match.Value
                });

                linksData.Add(lastUpdateLinkData);
            }

        }

        return linksData.ToArray();
    }

    public async Task<LinkData> GetLastUpdateLinkData(LinkData linkData)
    {
        var client = new HttpClient();
        var response = await client.GetAsync(linkData.Link + "/commits");
        var httpPage = await response.Content.ReadAsStringAsync();

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(httpPage);

        var lastCommit = htmlDoc.DocumentNode.SelectSingleNode("//a[@class=\"Link--primary text-bold js-navigation-open markdown-title\"]");

        var timestampLastUpdateNode = htmlDoc.DocumentNode.SelectSingleNode("//relative-time");
        var timestampLastUpdate = timestampLastUpdateNode.GetAttributeValue("datetime", null);
        var datetimeLastUpdate = DateTimeOffset.Parse(timestampLastUpdate);

        var newLinkData = new LinkData
        {
            Link = linkData.Link,
            LastCommit = lastCommit.InnerText,
            LastGithubUpdate = datetimeLastUpdate
        };

        return newLinkData;
    }

    public bool Save(HomeworksCache cache)
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

    public HomeworksCache Get()
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


