namespace StackSifter.Feed;

using CodeHollow.FeedReader;
using System.Net.Http;

public class MetaStackOverflowFeedService : IFeedService
{
    public async Task<List<Post>> FetchPostsSinceAsync(DateTime since)
    {
        var feedUrl = "https://meta.stackoverflow.com/feeds";
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("StackSifterBot/1.0 (+https://github.com/gcasar/stack-sifter)");
        var xml = await httpClient.GetStringAsync(feedUrl);
        // Replace problematic entities (e.g., &bull;) with safe equivalents
        xml = xml.Replace("&bull;", "•");
        // Add more replacements as needed for other entities
        var feed = FeedReader.ReadFromString(xml);
        var posts = feed.Items
            .Where(item => item.PublishingDate != null && item.PublishingDate > since)
            .OrderBy(item => item.PublishingDate)
            .Select(item => new Post
            {
                Published = item.PublishingDate!.Value
                // Add other property mappings as needed
            })
            .ToList();
        // Return in batches of 10 (or less if not enough)
        return posts.Take((posts.Count / 10) * 10).ToList();
    }
}
