using StackSifter.Configuration;
using StackSifter.Feed;
using StackSifter.Notifications;

namespace StackSifter;

/// <summary>
/// Orchestrates the entire stack sifting process using configuration:
/// - Processes multiple feeds
/// - Applies multiple rules with different criteria
/// - Sends notifications to multiple targets
/// </summary>
public class ConfigurableStackSifterService
{
    private readonly StackSifterConfig _config;
    private readonly string _openAiApiKey;

    public ConfigurableStackSifterService(StackSifterConfig config, string openAiApiKey)
    {
        _config = config;
        _openAiApiKey = openAiApiKey;
    }

    /// <summary>
    /// Processes all configured feeds and applies all rules, returning aggregated results.
    /// </summary>
    public async Task<ProcessingResult> ProcessAsync(DateTime since)
    {
        var allMatches = new List<MatchedPost>();
        var totalProcessed = 0;
        DateTime? lastCreated = null;

        // Process each feed
        foreach (var feedUrl in _config.Feeds)
        {
            var feed = new StackOverflowRSSFeed(feedUrl);
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
                var sifter = CreateSifter(rule);
                var notifiers = CreateNotifiers(rule);

                foreach (var post in posts)
                {
                    var isMatch = await sifter.IsMatch(post);

                    if (isMatch)
                    {
                        // Record the match
                        allMatches.Add(new MatchedPost(post, rule.Prompt, rule.Notify));

                        // Send notifications
                        foreach (var notifier in notifiers)
                        {
                            await notifier.NotifyAsync(post, rule.Prompt);
                        }
                    }
                }
            }
        }

        return new ProcessingResult(totalProcessed, lastCreated, allMatches);
    }

    /// <summary>
    /// Creates the appropriate sifter based on the rule configuration.
    /// </summary>
    private IPostSifter CreateSifter(SiftingRule rule)
    {
        var sifterType = rule.SifterType?.ToLowerInvariant() ?? "llm";

        return sifterType switch
        {
            "llm" => new OpenAILLMSifter(_openAiApiKey, rule.Prompt),
            "all" => new AllMatchPostSifter(),
            // Future: Add regex, tag-based, etc.
            _ => throw new InvalidOperationException($"Unknown sifter type: {rule.SifterType}")
        };
    }

    /// <summary>
    /// Creates notifiers for each notification target in the rule.
    /// </summary>
    private List<INotifier> CreateNotifiers(SiftingRule rule)
    {
        var notifiers = new List<INotifier>();

        foreach (var target in rule.Notify)
        {
            if (!string.IsNullOrEmpty(target.Slack))
            {
                notifiers.Add(new SlackNotifier(target.Slack));
            }
            else if (!string.IsNullOrEmpty(target.Webhook))
            {
                // Future: WebhookNotifier
                notifiers.Add(new ConsoleNotifier($"Webhook: {target.Webhook}"));
            }
            else if (!string.IsNullOrEmpty(target.Email))
            {
                // Future: EmailNotifier
                notifiers.Add(new ConsoleNotifier($"Email: {target.Email}"));
            }
            else
            {
                // Fallback to console if nothing configured
                notifiers.Add(new ConsoleNotifier(target.GetDescription()));
            }
        }

        return notifiers;
    }
}

/// <summary>
/// Result of processing all feeds and rules.
/// </summary>
public record ProcessingResult(
    int TotalProcessed,
    DateTime? LastCreated,
    List<MatchedPost> Matches
);

/// <summary>
/// Represents a post that matched a rule and will be/was notified.
/// </summary>
public record MatchedPost(
    Post Post,
    string MatchReason,
    List<NotificationTarget> NotificationTargets
);
