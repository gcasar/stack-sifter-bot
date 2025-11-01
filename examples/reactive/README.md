# Reactive Extensions (Rx.NET) Examples

This directory contains example implementations showing how Stack Sifter Bot could be rewritten using Reactive Extensions for .NET (System.Reactive).

## Contents

### 📄 Documentation
- **[COMPARISON.md](COMPARISON.md)** - Side-by-side comparison of current vs reactive implementations
- **[PROS_AND_CONS.md](PROS_AND_CONS.md)** - Detailed analysis of benefits and drawbacks

### 💻 Code Examples
- **[IObservablePostsFeed.cs](IObservablePostsFeed.cs)** - Interface definition for reactive feeds
- **[ReactiveStackOverflowRSSFeed.cs](ReactiveStackOverflowRSSFeed.cs)** - Reactive RSS feed implementation
- **[ReactiveStackSifterService.cs](ReactiveStackSifterService.cs)** - Reactive processing pipeline
- **[ReactiveNotificationService.cs](ReactiveNotificationService.cs)** - Multi-consumer notification pattern

## Quick Start

### Understanding the Reactive Approach

The current implementation uses Task-based async/await:

```csharp
// Current: Batch processing
var posts = await feed.FetchPostsSinceAsync(since);
var matches = await Task.WhenAll(posts.Select(p => CheckMatch(p)));
```

The reactive approach uses observable streams:

```csharp
// Reactive: Streaming pipeline
var matches = feed.ObservePosts(since)
    .SelectMany(post => CheckMatchReactive(post))
    .Where(result => result.IsMatch);
```

### Key Benefits

1. **Backpressure Control** - Limit concurrent API calls to avoid rate limits
2. **Error Resilience** - Automatic retry and graceful error handling
3. **Composability** - Easy to add caching, metrics, notifications
4. **Streaming** - Process posts as they arrive, not all at once

### Key Challenges

1. **Learning Curve** - Rx.NET has 100+ operators and new concepts
2. **Debugging** - Stack traces are harder to read
3. **Complexity** - May be overkill for simple batch processing

## Recommended Approach

### Phase 0: Quick Win (No Rx.NET)
Add simple throttling to current implementation:

```csharp
private readonly SemaphoreSlim _throttle = new(5, 5);

await _throttle.WaitAsync();
try
{
    await Task.Delay(200); // 5 calls/second
    return await CheckMatch(post, rule);
}
finally
{
    _throttle.Release();
}
```

**This solves 80% of the problem with 5% of the effort.**

### Phase 1: Streaming Foundation
Use `IAsyncEnumerable` for lightweight streaming:

```csharp
public async IAsyncEnumerable<Post> StreamPostsAsync(DateTime since)
{
    var xml = await _httpClient.GetStringAsync(_feedUrl);
    var feed = FeedReader.ReadFromString(xml);

    foreach (var item in feed.Items.Where(i => i.PublishingDate > since))
    {
        yield return MapToPost(item);
    }
}
```

### Phase 2: Full Reactive (If Needed)
Implement full Rx.NET pipelines as shown in the example files.

## Running the Examples

These are example/reference implementations and are not runnable as-is. To experiment:

1. **Add Rx.NET to your project:**
   ```bash
   dotnet add package System.Reactive
   ```

2. **Copy example code** into your project

3. **Adapt to your needs** - these are templates, not production code

## Learning Resources

### Rx.NET Essentials
- [Introduction to Rx.NET](http://introtorx.com/) - Comprehensive guide
- [ReactiveX.io](http://reactivex.io/) - Concepts apply across all Rx implementations
- [System.Reactive GitHub](https://github.com/dotnet/reactive) - Official repository

### Key Operators for Stack Sifter Bot

| Operator | Purpose | Example Use |
|----------|---------|-------------|
| `Observable.Create()` | Create custom observable | Wrap RSS feed fetching |
| `Merge()` | Combine streams | Merge multiple feeds |
| `SelectMany()` | Async map + flatten | API calls with concurrency limit |
| `Throttle()` | Rate limiting | Control API call frequency |
| `Retry()` | Error recovery | Retry failed API calls |
| `Buffer()` | Batch items | Group posts for notifications |
| `Distinct()` | Deduplication | Remove duplicate posts |
| `Where()` | Filter | Keep only matched posts |

### Testing Reactive Code

Use `TestScheduler` for deterministic time-based testing:

```csharp
var scheduler = new TestScheduler();
var source = Observable.Interval(TimeSpan.FromSeconds(1), scheduler);

// Fast-forward 10 seconds instantly
scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);

// Assert results - no actual waiting!
```

## Architecture Diagrams

### Current Architecture
```
GitHub Actions (Hourly)
    ↓
Fetch All Feeds (Parallel)
    ↓
Load All Posts into Memory
    ↓
Evaluate All Rules (Parallel, No Throttle)
    ↓
Output JSON
```

**Issues:**
- No rate limiting (can hit API limits)
- All-or-nothing (one error fails entire run)
- High memory usage

### Reactive Architecture
```
Observable Feeds (Cold)
    ↓
Merge Streams
    ↓
Stream Posts (One at a Time)
    ↓
Evaluate Rules (Throttled, Max Concurrency)
    ↓
Split Stream
    ├→ JSON Output
    ├→ Slack Notifications
    └→ Metrics
```

**Benefits:**
- Built-in throttling prevents rate limits
- Errors don't stop other posts
- Streaming reduces memory
- Easy to add consumers (Slack, metrics, etc.)

## Decision Matrix

### Use Reactive If:
- ✅ Hitting API rate limits
- ✅ Need Slack notifications or multiple outputs
- ✅ Want better error handling
- ✅ Plan to scale (more feeds, more posts)
- ✅ Team willing to learn Rx.NET

### Don't Use Reactive If:
- ❌ Current implementation works fine
- ❌ No time for learning curve
- ❌ Simplicity is priority #1
- ❌ No rate limit issues

## Next Steps

1. **Read** [COMPARISON.md](COMPARISON.md) for detailed code comparisons
2. **Review** [PROS_AND_CONS.md](PROS_AND_CONS.md) for comprehensive analysis
3. **Start small** - Add simple throttling first (Phase 0)
4. **Experiment** - Try `IAsyncEnumerable` before full Rx.NET
5. **Measure** - Profile before and after to validate improvements

## Questions?

This analysis was created to evaluate reactive programming for Stack Sifter Bot. The examples are illustrative and would need adaptation for production use.

Key considerations:
- **Performance**: Rx has overhead but is often negligible for I/O-bound workloads
- **Complexity**: Rx is powerful but has a learning curve
- **Incremental adoption**: You can use Rx in parts of your app, not all-or-nothing

**Recommendation**: Start with Phase 0 (simple throttling) and only move to Rx if you need its advanced features.

---

**Last Updated:** 2025-11-01
**Status:** Proof of Concept / Reference Implementation
