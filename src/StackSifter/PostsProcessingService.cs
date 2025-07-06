using StackSifter.Feed;

namespace StackSifter;

public class PostsProcessingService(IPostsFeed Feed, IPostSifter Sifter)
{
    public async Task<List<Post>> FetchAndFilterPostsAsync(DateTime since)
    {
        var posts = await Feed.FetchPostsSinceAsync(since);
        var matches = new List<Post>();
        foreach (var post in posts)
        {
            if (await Sifter.IsMatch(post))
            {
                matches.Add(post);
            }
        }
        return matches;
    }
}
