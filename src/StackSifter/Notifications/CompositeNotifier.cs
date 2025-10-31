using StackSifter.Feed;

namespace StackSifter.Notifications;

/// <summary>
/// Composite notifier that sends notifications to multiple targets.
/// Useful for broadcasting the same notification to multiple channels (e.g., Slack + email).
/// </summary>
public class CompositeNotifier : INotifier
{
    private readonly List<INotifier> _notifiers;

    public CompositeNotifier(params INotifier[] notifiers)
    {
        _notifiers = notifiers.ToList();
    }

    public CompositeNotifier(IEnumerable<INotifier> notifiers)
    {
        _notifiers = notifiers.ToList();
    }

    public void AddNotifier(INotifier notifier)
    {
        _notifiers.Add(notifier);
    }

    public async Task NotifyAsync(Post post, string matchReason, CancellationToken cancellationToken = default)
    {
        // Send notifications to all targets in parallel
        var tasks = _notifiers.Select(notifier => notifier.NotifyAsync(post, matchReason, cancellationToken));
        await Task.WhenAll(tasks);
    }
}
