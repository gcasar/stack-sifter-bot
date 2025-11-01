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
        var allMatches = new List<MatchedPost>();
        var totalProcessed = 0;
        DateTime? lastCreated = null;

        // Process each feed
        foreach (var feedUrl in _config.Feeds)
        {
            var feed = new StackOverflowRSSFeed(feedUrl: feedUrl);
            var posts = await feed.FetchPostsSinceAsync(since);

            totalProcessed += posts.Count;

            // Track the most recent post timestamp across all feeds
            if (posts.Count > 0)
            {
                var feedLastCreated = posts.Max(p => p.Published);
                if (!lastCreated.HasValue || feedLastCreated > lastCreated.Value)
                {
                    lastCreated = feedLastCreated;
                }
            }

            // Apply each rule to each post
            foreach (var rule in _config.Rules)
            {
                var sifter = new OpenAILLMSifter(_openAiApiKey, rule.Prompt);

                foreach (var post in posts)
                {
                    var isMatch = await sifter.IsMatch(post);

                    if (isMatch)
                    {
                        allMatches.Add(new MatchedPost(post, rule.Prompt));
                    }
                }
            }
        }

        return new ProcessingResult(totalProcessed, lastCreated, allMatches);
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
