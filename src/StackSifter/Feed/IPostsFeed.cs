namespace StackSifter.Feed;

public interface IPostsFeed
{
    Task<List<Post>> FetchPostsSinceAsync(DateTime since);
}

public record Post(DateTime Published, string Title, string Brief, List<string> Tags);
