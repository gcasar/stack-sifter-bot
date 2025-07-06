namespace StackSifter.Feed;

using CodeHollow.FeedReader;
using System.Net.Http;

public class StackOverflowRSSFeed : IPostsFeed
{
    private readonly HttpClient _httpClient;
    private readonly string _feedUrl;

    public StackOverflowRSSFeed(HttpClient? httpClient = null, string? feedUrl = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _feedUrl = feedUrl ?? "https://stackoverflow.com/feeds";
    }

    public async Task<List<Post>> FetchPostsSinceAsync(DateTime since)
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("StackSifterBot/1.0 (+https://github.com/gcasar/stack-sifter)");
        var xml = await _httpClient.GetStringAsync(_feedUrl);
        // Replace problematic entities (e.g., &bull;) with safe equivalents
        xml = xml.Replace("&bull;", "•");
        // Add more replacements as needed for other entities
        var feed = FeedReader.ReadFromString(xml);
        var posts = feed.Items
            .Where(item => item.PublishingDate != null && item.PublishingDate > since)
            .OrderBy(item => item.PublishingDate)
            .Select(item =>
                new Post(
                    item.PublishingDate!.Value,
                    item.Title ?? string.Empty,
                    item.Description ?? string.Empty,
                    item.Categories?.ToList() ?? new List<string>()
                )
            )
            .ToList();

        // Todo - batching
        return posts;
    }
}
