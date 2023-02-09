// todo <> section in config

using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace CleannetCode_bot.Features.Homeworks;

public partial class DiscussionMessagesRepository
{

    public static async Task<DiscussionMessages[]> GetMessagesFromDiscussion(string organizationName, string repositoryName, int discussionID)
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
                // maybe todo: lastEditedTimestampNode = ...;

                var datetimeCreateNode = DateTimeOffset.Parse(timestampCreateNode);

                if (authorNameNode != null && messageNode != null)
                {
                    var messageText = RegexDeleteUnicodeSymbols().Replace(messageNode.InnerText, string.Empty);

                    discussionMessages.Add(new DiscussionMessages(
                        authorNameNode.InnerText,
                        messageText,
                        datetimeCreateNode));
                }
            }
        }

        return discussionMessages.ToArray();
    }

    [GeneratedRegex("\\\\u\\d{4}")]
    private static partial Regex RegexDeleteUnicodeSymbols();
}


