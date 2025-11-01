using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using StackSifter.Configuration;

namespace StackSifter.Reactive;

/// <summary>
/// Reactive version of ConfigurableStackSifterService.
/// Uses Rx.NET to create composable, backpressure-aware processing pipelines.
/// </summary>
public class ReactiveStackSifterService
{
    private readonly StackSifterConfig _config;
    private readonly IPostSifter _sifter;

    public ReactiveStackSifterService(StackSifterConfig config, IPostSifter sifter)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _sifter = sifter ?? throw new ArgumentNullException(nameof(sifter));
    }

    /// <summary>
    /// Process all configured feeds and return matched posts.
    /// This maintains the same interface as the current implementation but uses
    /// reactive pipelines internally for better resource management.
    /// </summary>
    public async Task<ProcessingResult> ProcessAsync(DateTime since)
    {
        var pipeline = CreateProcessingPipeline(since);

        // Collect all results into a list
        var matches = await pipeline.ToList();

        return new ProcessingResult
        {
            TotalProcessed = matches.Count,
            MatchingPosts = matches.ToList(),
            LastCreated = matches.Any()
                ? matches.Max(m => m.Post.CreatedDate)
                : since
        };
    }

    /// <summary>
    /// Creates the reactive processing pipeline.
    ///
    /// Pipeline stages:
    /// 1. Create observables for each feed
    /// 2. Merge all feeds into single stream
    /// 3. Add deduplication (optional but recommended)
    /// 4. Evaluate each post against all rules
    /// 5. Throttle API calls to respect rate limits
    /// 6. Filter to only matched posts
    /// 7. Add retry logic for transient failures
    /// </summary>
    private IObservable<MatchedPost> CreateProcessingPipeline(DateTime since)
    {
        // Stage 1 & 2: Create and merge all feed observables
        var allPosts = _config.Feeds
            .Select(feedUrl => new ReactiveStackOverflowRSSFeed(feedUrl).ObservePosts(since))
            .Merge(maxConcurrent: 3); // Fetch max 3 feeds concurrently

        // Stage 3: Deduplicate (posts may appear in multiple feeds)
        var uniquePosts = allPosts.Distinct(p => p.Id);

        // Stage 4-6: Evaluate rules with backpressure
        var matches = uniquePosts
            .SelectMany(post => EvaluatePostAgainstRules(post))
            .Where(result => result != null);

        // Stage 7: Add retry logic for transient failures
        return matches.Retry(3);
    }

    /// <summary>
    /// Evaluates a single post against all configured rules.
    /// Returns an observable of matched posts (may be empty if no rules match).
    ///
    /// Key reactive features:
    /// - Throttle: Limits API calls per second
    /// - SelectMany with maxConcurrent: Limits parallel API calls
    /// - Catch: Handles errors gracefully without stopping pipeline
    /// </summary>
    private IObservable<MatchedPost?> EvaluatePostAgainstRules(Post post)
    {
        return _config.Rules
            .ToObservable()
            .SelectMany(
                async rule =>
                {
                    try
                    {
                        var isMatch = await _sifter.IsMatch(post, rule);
                        if (isMatch)
                        {
                            return new MatchedPost(
                                Post: post,
                                Rule: rule.Name,
                                MatchReason: $"Matched rule: {rule.Name}"
                            );
                        }
                        return null;
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail entire pipeline
                        Console.Error.WriteLine($"Error evaluating rule {rule.Name} for post {post.Id}: {ex.Message}");
                        return null;
                    }
                },
                maxConcurrent: 5  // Max 5 concurrent API calls
            )
            .Throttle(TimeSpan.FromMilliseconds(200)); // Max 5 calls/second
    }

    /// <summary>
    /// Alternative implementation with more advanced features:
    /// - Batching for efficient API usage
    /// - Hot observable for notifications
    /// - Metrics collection
    /// </summary>
    public IObservable<MatchedPost> CreateAdvancedPipeline(DateTime since)
    {
        var allPosts = _config.Feeds
            .Select(feedUrl => new ReactiveStackOverflowRSSFeed(feedUrl).ObservePosts(since))
            .Merge(maxConcurrent: 3);

        return allPosts
            // Add logging
            .Do(post => Console.WriteLine($"Processing post: {post.Id}"))

            // Deduplicate
            .Distinct(p => p.Id)

            // Batch posts for efficient processing
            .Buffer(TimeSpan.FromSeconds(10), count: 20) // Batch every 10s or 20 posts

            // Process each batch
            .SelectMany(batch =>
                Observable.FromAsync(() => ProcessBatchAsync(batch))
            )

            // Flatten results
            .SelectMany(results => results)

            // Add metrics
            .Do(match => Console.WriteLine($"Match found: {match.Post.Title}"))

            // Error handling
            .Catch<MatchedPost, Exception>(ex =>
            {
                Console.Error.WriteLine($"Pipeline error: {ex.Message}");
                return Observable.Empty<MatchedPost>();
            })

            // Retry transient failures
            .Retry(3);
    }

    private async Task<MatchedPost[]> ProcessBatchAsync(System.Collections.Generic.IList<Post> posts)
    {
        var matches = new System.Collections.Generic.List<MatchedPost>();

        foreach (var post in posts)
        {
            foreach (var rule in _config.Rules)
            {
                try
                {
                    if (await _sifter.IsMatch(post, rule))
                    {
                        matches.Add(new MatchedPost(
                            Post: post,
                            Rule: rule.Name,
                            MatchReason: $"Matched rule: {rule.Name}"
                        ));
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error in batch processing: {ex.Message}");
                }
            }
        }

        return matches.ToArray();
    }
}

/// <summary>
/// Result of reactive processing (same as current implementation)
/// </summary>
public record ProcessingResult
{
    public int TotalProcessed { get; init; }
    public System.Collections.Generic.List<MatchedPost> MatchingPosts { get; init; } = new();
    public DateTime LastCreated { get; init; }
}

/// <summary>
/// A post that matched a sifting rule
/// </summary>
public record MatchedPost(
    Post Post,
    string Rule,
    string MatchReason
);
