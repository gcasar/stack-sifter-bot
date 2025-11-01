using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace StackSifter.Reactive;

/// <summary>
/// Demonstrates using hot observables for multi-consumer notification scenarios.
///
/// Key concept: A "Subject" is both an observer and observable, allowing multiple
/// subscribers to receive the same events. This is perfect for broadcasting
/// matches to multiple consumers (Slack, metrics, console, database, etc.)
/// </summary>
public class ReactiveNotificationService
{
    private readonly Subject<MatchedPost> _matchSubject;
    private readonly ISlackNotifier _slackNotifier;
    private readonly IMetricsCollector _metricsCollector;

    public ReactiveNotificationService(
        ISlackNotifier slackNotifier,
        IMetricsCollector metricsCollector)
    {
        _matchSubject = new Subject<MatchedPost>();
        _slackNotifier = slackNotifier;
        _metricsCollector = metricsCollector;

        SetupSubscriptions();
    }

    /// <summary>
    /// Setup multiple consumers for the match stream.
    /// Each subscriber operates independently and can have different logic.
    /// </summary>
    private void SetupSubscriptions()
    {
        // Subscriber 1: Batch Slack notifications every 5 minutes
        _matchSubject
            .Buffer(TimeSpan.FromMinutes(5))
            .Where(batch => batch.Any())
            .Subscribe(async batch =>
            {
                try
                {
                    await _slackNotifier.SendBatchAsync(batch);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Slack notification failed: {ex.Message}");
                }
            });

        // Subscriber 2: Immediate console output
        _matchSubject
            .Subscribe(match =>
            {
                Console.WriteLine($"[MATCH] {match.Post.Title} ({match.Rule})");
            });

        // Subscriber 3: Collect metrics
        _matchSubject
            .Buffer(TimeSpan.FromMinutes(1))
            .Subscribe(batch =>
            {
                _metricsCollector.RecordMatches(batch.Count);
            });

        // Subscriber 4: High-priority alerts (separate Slack channel for critical matches)
        _matchSubject
            .Where(match => match.Rule.Contains("CRITICAL"))
            .Subscribe(async match =>
            {
                await _slackNotifier.SendImmediateAlert(match);
            });

        // Subscriber 5: Deduplicated daily digest
        _matchSubject
            .Window(TimeSpan.FromDays(1))
            .Subscribe(async window =>
            {
                var dailyMatches = await window.Distinct(m => m.Post.Id).ToList();
                if (dailyMatches.Any())
                {
                    await _slackNotifier.SendDailyDigest(dailyMatches);
                }
            });
    }

    /// <summary>
    /// Publish a matched post to all subscribers
    /// </summary>
    public void PublishMatch(MatchedPost match)
    {
        _matchSubject.OnNext(match);
    }

    /// <summary>
    /// Connect an observable stream to the notification system
    /// </summary>
    public IDisposable ConnectMatchStream(IObservable<MatchedPost> matchStream)
    {
        return matchStream.Subscribe(_matchSubject);
    }

    /// <summary>
    /// Cleanup subscriptions
    /// </summary>
    public void Dispose()
    {
        _matchSubject.OnCompleted();
        _matchSubject.Dispose();
    }
}

/// <summary>
/// Example: Complete reactive pipeline with notifications
/// </summary>
public class CompleteReactivePipeline
{
    public async Task RunAsync()
    {
        var config = new StackSifterConfig(); // Load from file
        var sifter = new OpenAILLMSifter(); // Initialize
        var slackNotifier = new SlackNotifier(); // Initialize
        var metricsCollector = new MetricsCollector(); // Initialize

        // Create notification service (sets up all subscribers)
        using var notificationService = new ReactiveNotificationService(
            slackNotifier,
            metricsCollector
        );

        // Create processing pipeline
        var since = DateTime.UtcNow.AddHours(-1);
        var matches = CreatePipeline(config, sifter, since);

        // Connect pipeline to notification service
        var subscription = notificationService.ConnectMatchStream(matches);

        // Also collect results for JSON output
        var results = await matches.ToList();

        // Output results
        Console.WriteLine($"Total matches: {results.Count}");

        // Cleanup
        subscription.Dispose();
    }

    private IObservable<MatchedPost> CreatePipeline(
        StackSifterConfig config,
        IPostSifter sifter,
        DateTime since)
    {
        return config.Feeds
            .Select(feedUrl => new ReactiveStackOverflowRSSFeed(feedUrl).ObservePosts(since))
            .Merge(maxConcurrent: 3)
            .Distinct(p => p.Id)
            .SelectMany(post => EvaluateRules(post, config.Rules, sifter))
            .Where(result => result != null)
            .Select(result => result!); // Non-null assertion after Where
    }

    private IObservable<MatchedPost?> EvaluateRules(
        Post post,
        SiftingRule[] rules,
        IPostSifter sifter)
    {
        return rules
            .ToObservable()
            .SelectMany(
                async rule =>
                {
                    var isMatch = await sifter.IsMatch(post, rule);
                    return isMatch
                        ? new MatchedPost(post, rule.Name, $"Matched: {rule.Name}")
                        : null;
                },
                maxConcurrent: 5
            )
            .Throttle(TimeSpan.FromMilliseconds(200));
    }
}

// Mock interfaces for example
public interface ISlackNotifier
{
    Task SendBatchAsync(IList<MatchedPost> matches);
    Task SendImmediateAlert(MatchedPost match);
    Task SendDailyDigest(IList<MatchedPost> matches);
}

public interface IMetricsCollector
{
    void RecordMatches(int count);
}

public class SlackNotifier : ISlackNotifier
{
    public Task SendBatchAsync(IList<MatchedPost> matches)
    {
        Console.WriteLine($"Sending {matches.Count} matches to Slack...");
        return Task.CompletedTask;
    }

    public Task SendImmediateAlert(MatchedPost match)
    {
        Console.WriteLine($"CRITICAL ALERT: {match.Post.Title}");
        return Task.CompletedTask;
    }

    public Task SendDailyDigest(IList<MatchedPost> matches)
    {
        Console.WriteLine($"Daily digest: {matches.Count} matches");
        return Task.CompletedTask;
    }
}

public class MetricsCollector : IMetricsCollector
{
    public void RecordMatches(int count)
    {
        Console.WriteLine($"Metrics: {count} matches in last minute");
    }
}
