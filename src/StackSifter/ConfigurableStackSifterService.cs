using StackSifter.Configuration;
using StackSifter.Feed;

namespace StackSifter;

public class ConfigurableStackSifterService
{
    private readonly StackSifterConfig _config;
    private readonly string _openAiApiKey;

    public ConfigurableStackSifterService(StackSifterConfig config, string openAiApiKey)
    {
        _config = config;
        _openAiApiKey = openAiApiKey;
    }

    public async Task<ProcessingResult> ProcessAsync(DateTime since)
    {
        var feedResults = await Task.WhenAll(
            _config.Feeds.Select(async feedUrl =>
            {
                var feed = new StackOverflowRSSFeed(feedUrl: feedUrl);
                var posts = await feed.FetchPostsSinceAsync(since);

                var matches = await Task.WhenAll(
                    from rule in _config.Rules
                    from post in posts
                    select CheckMatchAsync(post, rule)
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

    private async Task<MatchedPost?> CheckMatchAsync(Post post, SiftingRule rule)
    {
        var sifter = new OpenAILLMSifter(_openAiApiKey, rule.Prompt);
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
