# Side-by-Side Comparison: Current vs Reactive

## Example Scenario
- 3 RSS feeds
- 10 posts per feed (30 total)
- 2 sifting rules
- Total operations: 60 rule evaluations (30 posts × 2 rules)

---

## Current Implementation

### ConfigurableStackSifterService.cs (Current)

```csharp
public async Task<ProcessingResult> ProcessAsync(DateTime since)
{
    var sifters = _config.Rules
        .Select(rule => new { Rule = rule, Sifter = CreateSifter(rule) })
        .ToList();

    // Process all feeds in parallel
    var feedResults = await Task.WhenAll(
        _config.Feeds.Select(async feedUrl =>
        {
            var feed = new StackOverflowRSSFeed(feedUrl: feedUrl);

            // Fetch all posts at once (loads into memory)
            var posts = await feed.FetchPostsSinceAsync(since);

            // Evaluate ALL combinations in parallel (no throttling!)
            var matches = await Task.WhenAll(
                from ruleSifter in sifters
                from post in posts
                select CheckMatchAsync(post, ruleSifter.Rule, ruleSifter.Sifter)
            );

            return new
            {
                Posts = posts,
                Matches = matches.Where(m => m != null).ToList()
            };
        })
    );

    // Aggregate all results
    var allMatches = feedResults.SelectMany(r => r.Matches).ToList();
    var allPosts = feedResults.SelectMany(r => r.Posts).ToList();

    return new ProcessingResult
    {
        TotalProcessed = allPosts.Count,
        MatchingPosts = allMatches,
        LastCreated = allPosts.Max(p => p.CreatedDate)
    };
}
```

### Execution Flow (Current)

```
Time  Action                           API Calls  Memory
0s    Start processing
0s    Fetch Feed 1, 2, 3 (parallel)    0          5 MB
2s    All feeds fetched                0          15 MB (30 posts)
2s    Start rule evaluation
2s    Spawn 60 parallel API calls      60         15 MB
5s    All API calls complete           0          18 MB
5s    Return results
```

### Characteristics (Current)

| Aspect | Value |
|--------|-------|
| Total time | ~5 seconds |
| Peak memory | ~18 MB |
| Peak API calls/second | ~20 calls/sec (burst at 2s mark) |
| Error handling | Fail fast - one error stops everything |
| Extensibility | Hard - tightly coupled |
| Testability | Medium - mocks required for timing |

---

## Reactive Implementation

### ReactiveStackSifterService.cs (Proposed)

```csharp
public async Task<ProcessingResult> ProcessAsync(DateTime since)
{
    var pipeline = CreateProcessingPipeline(since);
    var matches = await pipeline.ToList();

    return new ProcessingResult
    {
        TotalProcessed = matches.Count,
        MatchingPosts = matches,
        LastCreated = matches.Max(m => m.Post.CreatedDate)
    };
}

private IObservable<MatchedPost> CreateProcessingPipeline(DateTime since)
{
    // Merge all feeds into single stream
    var allPosts = _config.Feeds
        .Select(url => new ReactiveStackOverflowRSSFeed(url).ObservePosts(since))
        .Merge(maxConcurrent: 3); // Limit concurrent feed fetches

    // Process with backpressure
    return allPosts
        .Distinct(p => p.Id) // Deduplicate
        .SelectMany(post => EvaluateRules(post))
        .Where(result => result != null)
        .Retry(3); // Retry on transient failures
}

private IObservable<MatchedPost?> EvaluateRules(Post post)
{
    return _config.Rules
        .ToObservable()
        .SelectMany(
            async rule =>
            {
                var isMatch = await _sifter.IsMatch(post, rule);
                return isMatch ? new MatchedPost(...) : null;
            },
            maxConcurrent: 5  // Max 5 concurrent API calls
        )
        .Throttle(TimeSpan.FromMilliseconds(200)); // Max 5 calls/second
}
```

### Execution Flow (Reactive)

```
Time  Action                           API Calls  Memory
0s    Start processing
0s    Subscribe to pipeline            0          2 MB
0s    Start fetching Feed 1, 2, 3      0          2 MB
1s    Feed 1 yields Post 1             0          3 MB
1s    Start evaluating Post 1          2          3 MB (throttled)
1.2s  Feed 1 yields Post 2             0          3 MB
1.4s  Start evaluating Post 2          2          4 MB
...   Streaming continues
10s   All posts processed              0          5 MB
```

### Characteristics (Reactive)

| Aspect | Value |
|--------|-------|
| Total time | ~10 seconds (slower due to throttling) |
| Peak memory | ~5 MB (streaming, not batch) |
| Peak API calls/second | ~5 calls/sec (controlled) |
| Error handling | Resilient - retries, continues on error |
| Extensibility | High - composable operators |
| Testability | High - TestScheduler for deterministic tests |

---

## Code Examples: Common Scenarios

### Scenario 1: Add Caching

**Current (Requires Refactoring)**
```csharp
public async Task<ProcessingResult> ProcessAsync(DateTime since)
{
    // Need to add caching logic throughout the method
    var sifters = _config.Rules.Select(...).ToList();

    var feedResults = await Task.WhenAll(
        _config.Feeds.Select(async feedUrl =>
        {
            var feed = new StackOverflowRSSFeed(feedUrl: feedUrl);
            var posts = await feed.FetchPostsSinceAsync(since);

            // How do we cache here? Need to refactor...
            var matches = await Task.WhenAll(...);
            return new { Posts = posts, Matches = matches };
        })
    );
    // ...
}
```

**Reactive (Add One Line)**
```csharp
private IObservable<MatchedPost> CreateProcessingPipeline(DateTime since)
{
    var allPosts = _config.Feeds
        .Select(url => new ReactiveStackOverflowRSSFeed(url).ObservePosts(since))
        .Merge(maxConcurrent: 3);

    return allPosts
        .Distinct(p => p.Id)
        .Do(p => _cache.Store(p))  // ← Add caching with one line!
        .SelectMany(post => EvaluateRules(post))
        .Where(result => result != null)
        .Retry(3);
}
```

---

### Scenario 2: Add Metrics

**Current (Requires Changes Throughout)**
```csharp
public async Task<ProcessingResult> ProcessAsync(DateTime since)
{
    _metrics.StartTimer();  // Manual instrumentation

    var sifters = _config.Rules.Select(...).ToList();
    var feedResults = await Task.WhenAll(...);

    _metrics.RecordPosts(allPosts.Count);  // Manual counting
    _metrics.RecordMatches(allMatches.Count);
    _metrics.StopTimer();

    return result;
}
```

**Reactive (Add Operators)**
```csharp
private IObservable<MatchedPost> CreateProcessingPipeline(DateTime since)
{
    var allPosts = _config.Feeds
        .Select(url => new ReactiveStackOverflowRSSFeed(url).ObservePosts(since))
        .Merge(maxConcurrent: 3);

    return allPosts
        .Distinct(p => p.Id)
        .Do(p => _metrics.IncrementPosts())  // ← Count posts
        .SelectMany(post => EvaluateRules(post))
        .Where(result => result != null)
        .Do(m => _metrics.IncrementMatches())  // ← Count matches
        .Retry(3);
}
```

---

### Scenario 3: Add Slack Notifications

**Current (Requires New Code Path)**
```csharp
public async Task<ProcessingResult> ProcessAsync(DateTime since)
{
    var sifters = _config.Rules.Select(...).ToList();
    var feedResults = await Task.WhenAll(...);
    var allMatches = feedResults.SelectMany(r => r.Matches).ToList();

    // Need to add notification logic here
    if (allMatches.Any())
    {
        await _slackClient.SendNotificationAsync(allMatches);
    }

    return new ProcessingResult
    {
        TotalProcessed = allPosts.Count,
        MatchingPosts = allMatches,
        LastCreated = allPosts.Max(p => p.CreatedDate)
    };
}
```

**Reactive (Subscribe Independently)**
```csharp
public async Task<ProcessingResult> ProcessAsync(DateTime since)
{
    var pipeline = CreateProcessingPipeline(since);

    // Setup notification subscriber (doesn't affect main pipeline)
    var notificationSubscription = pipeline
        .Buffer(TimeSpan.FromMinutes(5))
        .Where(batch => batch.Any())
        .Subscribe(async batch =>
        {
            await _slackClient.SendNotificationAsync(batch);
        });

    // Main result collection (independent)
    var matches = await pipeline.ToList();

    notificationSubscription.Dispose();

    return new ProcessingResult { ... };
}
```

---

### Scenario 4: Handle API Rate Limits

**Current (No Built-in Support)**
```csharp
public async Task<ProcessingResult> ProcessAsync(DateTime since)
{
    var sifters = _config.Rules.Select(...).ToList();

    // Need to manually implement throttling
    var semaphore = new SemaphoreSlim(5, 5);
    var matches = await Task.WhenAll(
        (from ruleSifter in sifters
         from post in posts
         select ThrottledCheckAsync(post, ruleSifter, semaphore))
    );
    // ...
}

private async Task<MatchedPost?> ThrottledCheckAsync(
    Post post,
    RuleSifter sifter,
    SemaphoreSlim semaphore)
{
    await semaphore.WaitAsync();
    try
    {
        await Task.Delay(100); // Rate limit
        return await CheckMatchAsync(post, sifter.Rule, sifter.Sifter);
    }
    finally
    {
        semaphore.Release();
    }
}
```

**Reactive (Built-in)**
```csharp
private IObservable<MatchedPost?> EvaluateRules(Post post)
{
    return _config.Rules
        .ToObservable()
        .SelectMany(
            async rule => await _sifter.IsMatch(post, rule),
            maxConcurrent: 5  // ← Built-in concurrency limit
        )
        .Throttle(TimeSpan.FromMilliseconds(200));  // ← Built-in throttle
}
```

---

### Scenario 5: Add Retry Logic

**Current (Manual Implementation)**
```csharp
private async Task<MatchedPost?> CheckMatchAsync(
    Post post,
    SiftingRule rule,
    IPostSifter sifter)
{
    var maxRetries = 3;
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            var isMatch = await sifter.IsMatch(post, rule);
            return isMatch ? new MatchedPost(...) : null;
        }
        catch (HttpRequestException ex) when (i < maxRetries - 1)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
        }
    }
    return null;
}
```

**Reactive (Built-in)**
```csharp
private IObservable<MatchedPost> CreateProcessingPipeline(DateTime since)
{
    var allPosts = _config.Feeds
        .Select(url => new ReactiveStackOverflowRSSFeed(url).ObservePosts(since))
        .Merge(maxConcurrent: 3);

    return allPosts
        .Distinct(p => p.Id)
        .SelectMany(post => EvaluateRules(post))
        .Where(result => result != null)
        .Retry(3);  // ← Built-in retry (or use RetryWhen for exponential backoff)
}
```

---

## Performance Comparison

### Scenario: 100 posts, 5 rules, 500 total evaluations

| Metric | Current | Reactive (Throttled) | Reactive (Balanced) |
|--------|---------|---------------------|---------------------|
| **Total Time** | 15s | 100s | 50s |
| **Peak Memory** | 45 MB | 8 MB | 12 MB |
| **API Calls/Second** | ~33 (burst) | ~5 (steady) | ~10 (controlled) |
| **First Result** | 15s (all or nothing) | 3s (streaming) | 5s (streaming) |
| **Error Recovery** | Fail entire run | Continue processing | Continue processing |
| **Rate Limit Errors** | Likely | None | Rare |

---

## Summary: When to Use Which

### Use Current (Task-based) When:
- ✅ Simple batch processing with no rate limits
- ✅ Team unfamiliar with Rx.NET
- ✅ Minimal extension requirements
- ✅ Speed is critical and API limits are not a concern

### Use Reactive When:
- ✅ Need backpressure/throttling for API rate limits
- ✅ Want composable pipelines (easy to add caching, metrics, etc.)
- ✅ Multiple consumers for same data (Slack + JSON + metrics)
- ✅ Require resilient error handling with retries
- ✅ Processing should continue even if some items fail
- ✅ Memory efficiency matters (large datasets)
- ✅ Real-time or streaming scenarios

### For Stack Sifter Bot:
**Recommendation: Hybrid Approach**

Keep Task-based for:
- Configuration loading
- CLI argument parsing
- Final result serialization

Use Reactive for:
- Feed fetching and merging
- Rule evaluation pipeline (throttling is critical)
- Slack notifications (hot observable)
- Metrics collection

This gives 80% of the benefits with 30% of the complexity.
