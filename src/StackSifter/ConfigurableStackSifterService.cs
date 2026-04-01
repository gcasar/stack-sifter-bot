using StackSifter.Configuration;
using StackSifter.Feed;

namespace StackSifter;

/// <summary>
/// Service that processes Stack Overflow posts from multiple RSS feeds and evaluates them against configured rules.
/// </summary>
public class ConfigurableStackSifterService
{
    private readonly StackSifterConfig _config;
    private readonly string _openAiApiKey;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the ConfigurableStackSifterService class.
    /// </summary>
    /// <param name="config">The configuration containing feeds and rules.</param>
    /// <param name="openAiApiKey">The OpenAI API key for LLM evaluation.</param>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    public ConfigurableStackSifterService(StackSifterConfig config, string openAiApiKey, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _openAiApiKey = openAiApiKey;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Processes all configured feeds and evaluates posts against configured rules.
    /// </summary>
    /// <param name="since">Only process posts published after this timestamp.</param>
    /// <returns>A processing result containing matched posts and statistics.</returns>
    public async Task<ProcessingResult> ProcessAsync(DateTime since)
    {
        // Create one sifter per rule (reused for all posts)
        var sifters = _config.Rules
            .Select(rule => new
            {
                Rule = rule,
                Sifter = new OpenAILLMSifter(_openAiApiKey, rule.Prompt, _httpClientFactory)
            })
            .ToList();

        var feedResults = await Task.WhenAll(
            _config.Feeds.Select(async feedUrl =>
            {
                var feed = new StackOverflowRSSFeed(_httpClientFactory, feedUrl: feedUrl);
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

/// <summary>
/// Represents the result of processing feeds.
/// </summary>
/// <param name="TotalProcessed">The total number of posts processed.</param>
/// <param name="LastCreated">The timestamp of the most recently created post, or null if no posts were found.</param>
/// <param name="Matches">List of posts that matched configured rules.</param>
public record ProcessingResult(
    int TotalProcessed,
    DateTime? LastCreated,
    List<MatchedPost> Matches
);

/// <summary>
/// Represents a post that matched a sifting rule.
/// </summary>
/// <param name="Post">The matched post.</param>
/// <param name="MatchReason">The reason why the post matched (typically the rule's prompt).</param>
public record MatchedPost(
    Post Post,
    string MatchReason
);
