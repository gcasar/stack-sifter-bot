using System;

namespace StackSifter.Reactive;

/// <summary>
/// Reactive version of IPostsFeed that streams posts as they become available
/// instead of returning all posts at once.
/// </summary>
public interface IObservablePostsFeed
{
    /// <summary>
    /// Creates an observable stream of posts published since the specified date.
    /// This is a "cold" observable - it only fetches data when subscribed to.
    /// </summary>
    /// <param name="since">Only include posts published after this date</param>
    /// <returns>Observable stream of posts</returns>
    IObservable<Post> ObservePosts(DateTime since);
}

/// <summary>
/// Post data model (same as existing)
/// </summary>
public record Post(
    string Id,
    string Title,
    string Link,
    DateTime CreatedDate,
    string Content,
    string[] Tags
);
