namespace StackSifter.Feed;

/// <summary>
/// Interface for fetching posts from a feed source.
/// </summary>
public interface IPostsFeed
{
    /// <summary>
    /// Fetches posts published after a given timestamp.
    /// </summary>
    /// <param name="since">Only return posts published after this timestamp.</param>
    /// <returns>List of posts published after the specified timestamp.</returns>
    Task<List<Post>> FetchPostsSinceAsync(DateTime since);
}

/// <summary>
/// Represents a post from a Stack Overflow RSS feed.
/// </summary>
/// <param name="Published">The timestamp when the post was published.</param>
/// <param name="Title">The post title.</param>
/// <param name="Brief">A brief description or excerpt of the post content.</param>
/// <param name="Tags">Tags associated with the post.</param>
/// <param name="Author">The post author.</param>
/// <param name="Url">The URL to the full post.</param>
public record Post(DateTime Published, string Title, string Brief, List<string> Tags, string Author, string Url);
