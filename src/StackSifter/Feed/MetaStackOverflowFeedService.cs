namespace StackSifter.Feed;

public class MetaStackOverflowFeedService : IFeedService
{
    public async Task<List<Post>> FetchPostsSinceAsync(DateTime since)
    {
        // TODO: Implement fetching from Meta Stack Overflow RSS feed in batches of 10
        // For now, return an empty list for test compilation
        await Task.CompletedTask;
        return new List<Post>();
    }
}
