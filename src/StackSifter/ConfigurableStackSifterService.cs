using StackSifter.Configuration;
using StackSifter.Feed;

namespace StackSifter;

public class ConfigurableStackSifterService
{
    private readonly StackSifterConfig _config;
    private readonly string _openAiApiKey;
    private readonly HttpClient _httpClient;

    public ConfigurableStackSifterService(StackSifterConfig config, string openAiApiKey, HttpClient? httpClient = null)
    {
        _config = config;
        _openAiApiKey = openAiApiKey;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<ProcessingResult> ProcessAsync(DateTime since)
    {
        // Create one sifter per rule (reused for all posts)
        var sifters = _config.Rules
            .Select(rule => new
            {
                Rule = rule,
                Sifter = new OpenAILLMSifter(_openAiApiKey, rule.Prompt, _httpClient)
            })
            .ToList();

        var feedResults = await Task.WhenAll(
            _config.Feeds.Select(async feedUrl =>
            {
                var feed = new StackOverflowRSSFeed(feedUrl: feedUrl);
                var posts = await feed.FetchPostsSinceAsync(since);

                var matches = await Task.WhenAll(
                    from ruleSifter in sifters
                    from post in posts
                    select CheckMatchAsync(post, ruleSifter.Rule, ruleSifter.Sifter)
                );

                return new
                {
                    Posts = posts,
                    Matches = matches.Where(m => m != null).Cast<MatchedPost>().ToList()
                };
            })
        );

        var allMatches = feedResults.SelectMany(r => r.Matches).ToList();
        var totalProcessed = feedResults.Sum(r => r.Posts.Count);
        var lastCreated = feedResults
            .SelectMany(r => r.Posts)
            .Select(p => p.Published)
            .DefaultIfEmpty()
            .Max();

        return new ProcessingResult(
            totalProcessed,
            lastCreated == default ? null : lastCreated,
            allMatches
        );
    }

    private async Task<MatchedPost?> CheckMatchAsync(Post post, SiftingRule rule, IPostSifter sifter)
    {
        var isMatch = await sifter.IsMatch(post);
        return isMatch ? new MatchedPost(post, rule.Prompt) : null;
    }
}

public record ProcessingResult(
    int TotalProcessed,
    DateTime? LastCreated,
    List<MatchedPost> Matches
);

public record MatchedPost(
    Post Post,
    string MatchReason
);
