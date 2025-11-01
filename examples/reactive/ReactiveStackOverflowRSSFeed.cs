using System;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeHollow.FeedReader;

namespace StackSifter.Reactive;

/// <summary>
/// Reactive implementation of Stack Overflow RSS feed reader.
/// Streams posts as they're parsed instead of loading all into memory.
/// </summary>
public class ReactiveStackOverflowRSSFeed : IObservablePostsFeed
{
    private readonly HttpClient _httpClient;
    private readonly string _feedUrl;

    public ReactiveStackOverflowRSSFeed(string feedUrl, HttpClient? httpClient = null)
    {
        _feedUrl = feedUrl ?? throw new ArgumentNullException(nameof(feedUrl));
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <summary>
    /// Creates a cold observable that fetches and streams posts when subscribed.
    /// Benefits:
    /// - Lazy evaluation (no work until subscribed)
    /// - Streaming (posts emitted as parsed, not all at once)
    /// - Cancellation support
    /// - Error propagation through observable pipeline
    /// </summary>
    public IObservable<Post> ObservePosts(DateTime since)
    {
        return Observable.Create<Post>(async (observer, cancellationToken) =>
        {
            try
            {
                // Fetch RSS feed
                var xml = await _httpClient.GetStringAsync(_feedUrl, cancellationToken);
                var feed = FeedReader.ReadFromString(xml);

                // Stream posts one at a time
                foreach (var item in feed.Items)
                {
                    // Check cancellation
                    if (cancellationToken.IsCancellationRequested)
                    {
                        observer.OnCompleted();
                        return;
                    }

                    // Filter by date
                    var publishDate = item.PublishingDate ?? DateTime.MinValue;
                    if (publishDate <= since)
                        continue;

                    // Parse and emit post
                    var post = new Post(
                        Id: item.Id,
                        Title: item.Title ?? "",
                        Link: item.Link ?? "",
                        CreatedDate: publishDate,
                        Content: item.Description ?? "",
                        Tags: item.Categories?.ToArray() ?? Array.Empty<string>()
                    );

                    observer.OnNext(post);
                }

                // Signal completion
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                // Propagate error through pipeline
                observer.OnError(ex);
            }
        });
    }

    /// <summary>
    /// Alternative: Use defer for even lazier evaluation
    /// The observable creation is deferred until subscription
    /// </summary>
    public IObservable<Post> ObservePostsDeferred(DateTime since)
    {
        return Observable.Defer(() => ObservePosts(since));
    }

    /// <summary>
    /// Polling version: Continuously polls the feed at intervals
    /// This creates a "hot" observable that emits new posts as they appear
    /// </summary>
    public IObservable<Post> ObservePostsContinuously(TimeSpan pollInterval)
    {
        var lastCheck = DateTime.UtcNow;

        return Observable
            .Interval(pollInterval)
            .SelectMany(async _ =>
            {
                var currentCheck = DateTime.UtcNow;
                var posts = await ObservePosts(lastCheck).ToList();
                lastCheck = currentCheck;
                return posts;
            })
            .SelectMany(posts => posts);
    }
}
