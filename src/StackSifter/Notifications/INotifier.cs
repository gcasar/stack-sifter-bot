using StackSifter.Feed;

namespace StackSifter.Notifications;

/// <summary>
/// Interface for sending notifications about matching posts.
/// </summary>
public interface INotifier
{
    /// <summary>
    /// Sends a notification about a post that matched a sifting rule.
    /// </summary>
    /// <param name="post">The post that matched.</param>
    /// <param name="matchReason">Description of why the post matched (e.g., the rule prompt).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    Task NotifyAsync(Post post, string matchReason, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of a notification operation.
/// </summary>
public record NotificationResult(
    bool Success,
    string Target,
    string? ErrorMessage = null
);
