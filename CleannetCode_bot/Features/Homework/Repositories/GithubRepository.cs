using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using CleannetCode_bot.Features.Homework.Models;

namespace CleannetCode_bot.Features.Homework.Repositories;
public class GithubRepository : IDisposable
{
    private readonly HttpClient httpClient;
    public GithubRepository(string token)
    {
        httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://api.github.com/graphql");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:106.0) Gecko/20100101 Firefox/106.0");
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
    }

    public static async Task<bool> IsValidToken(string token)
    {
        using var tempGithubRepository = new GithubRepository(token);

        var tempRepositories = await tempGithubRepository.GetStargazers("github", "docs");

        return tempRepositories != null && tempRepositories.TotalCount != null;
    }

    public async Task<CommentsDiscussions> GetCommentsDiscussions(
        string author,
        RepositoryMetaData[] repositories
    )
    {
        var result = new CommentsDiscussions();

        foreach (var repository in repositories)
        {
            foreach (var discussionID in repository.DiscussionsID)
            {
                var rawComments = await GetComments(author, repository.RepositoryName, discussionID);
                var comments = rawComments
                    .Select(rawComment => new Comment(
                        rawComment.Author?.Login ?? "",
                        rawComment.Body ?? "",
                        DateTimeOffset.Parse(rawComment.CreatedAt ?? ""),
                        rawComment.DatabaseId ?? 0))
                    .ToArray();

                var url = $"https://github.com/{author}/{repository.RepositoryName}/discussions/{discussionID}";
                result.Discussions.Add(url, comments);
            }
        }

        return result;
    }

    public async Task<HomeworkOnRepositories> GetCurrentHomeworkOnRepositories(
        string[] linksToRepositories
    )
    {
        var result = new HomeworkOnRepositories();

        foreach (var link in linksToRepositories)
        {
            var pattern = @"github\.com/(?<author>[^/]+)/(?<repository>[^/]+)/?$";
            var match = Regex.Match(link, pattern);
            if (!match.Success)
                continue;

            var author = match.Groups["author"].Value;
            var repositoryName = match.Groups["repository"].Value;

            var commit = await GetLastCommit(author, repositoryName);
            if (commit == null || commit == new Commit())
                continue;

            var stargazers = await GetStargazers(author, repositoryName);
            if (stargazers == null || stargazers == new Stargazers())
                continue;

            result.Repositories.Add(new RepositoryInfo(
                link,
                commit.Url ?? "",
                commit.Message ?? "",
                DateTimeOffset.Parse(commit.PushedDate ?? ""),
                stargazers.TotalCount
            ));
        }

        return result;
    }

    private async Task<JsonObject?> GraphqlRequest(StringContent requireJson)
    {
        var response = await httpClient.PostAsync("", requireJson);
        var responseJson = await response.Content.ReadAsStreamAsync();
        return JsonSerializer.Deserialize<JsonObject>(responseJson);
    }

    private async Task<CommentNode[]> GetComments(string author, string nameRepository, int numberDiscussion)
    {
        var queryBuilder = new QueryBuilder(
            "GetComments.graphql",
            new
            {
                owner = author,
                name = nameRepository,
                number = numberDiscussion
            }
        );

        var graphqlResponse = await GraphqlRequest(queryBuilder.GetJson());

        if (graphqlResponse == null || graphqlResponse.Count == 2)
            return Array.Empty<CommentNode>(); // string? error = graphqlResponse["message"].GetValue<string>();

        var node = graphqlResponse?["data"]?["repository"]?["discussion"]?["comments"]?["nodes"];

        if (node == null)
            return Array.Empty<CommentNode>();

        return JsonSerializer.Deserialize<CommentNode[]>(node.ToJsonString()) ?? Array.Empty<CommentNode>();
    }

    private async Task<Commit> GetLastCommit(string author, string nameRepository)
    {
        var queryBuilder = new QueryBuilder(
            "GetLastCommit.graphql",
            new
            {
                owner = author,
                name = nameRepository
            }
        );

        var graphqlResponse = await GraphqlRequest(queryBuilder.GetJson());

        if (graphqlResponse == null || graphqlResponse.Count == 2)
            return new Commit();


        var node = graphqlResponse?["data"]?["repository"]?["defaultBranchRef"]?["target"];

        if (node == null)
            return new Commit();

        return JsonSerializer.Deserialize<Commit>(node.ToJsonString()) ?? new Commit();
    }

    private async Task<Stargazers> GetStargazers(string author, string nameRepository)
    {
        var queryBuilder = new QueryBuilder(
            "GetStargazers.graphql",
            new
            {
                owner = author,
                name = nameRepository
            }
        );

        var graphqlResponse = await GraphqlRequest(queryBuilder.GetJson());

        if (graphqlResponse == null || graphqlResponse.Count == 2)
            return new Stargazers();

        var node = graphqlResponse?["data"]?["repository"]?["stargazers"];

        if (node == null)
            return new Stargazers();

        return JsonSerializer.Deserialize<Stargazers>(node.ToJsonString()) ?? new Stargazers();
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}
