namespace StackSifter.Feed;

public interface IFeedService
{
    Task<List<Post>> FetchPostsSinceAsync(DateTime since);
}

public class Post
{
    public DateTime Published { get; set; }
    // Add other relevant properties as needed
}
