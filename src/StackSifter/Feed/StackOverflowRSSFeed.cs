namespace StackSifter.Feed;

using CodeHollow.FeedReader;
using System.Net;
using System.Net.Http;

public class StackOverflowRSSFeed : IPostsFeed
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _feedUrl;

    public StackOverflowRSSFeed(IHttpClientFactory httpClientFactory, string? feedUrl = null)
    {
        _httpClientFactory = httpClientFactory;
        _feedUrl = feedUrl ?? "https://stackoverflow.com/feeds";
    }

    public async Task<List<Post>> FetchPostsSinceAsync(DateTime since)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("StackSifterBot/1.0 (+https://github.com/gcasar/stack-sifter)");
        var xml = await httpClient.GetStringAsync(_feedUrl);
        // Decode all HTML entities (e.g., &bull;, &nbsp;, &mdash;, etc.)
        xml = WebUtility.HtmlDecode(xml);
        var feed = FeedReader.ReadFromString(xml);

        var posts = feed.Items
            .Where(item => item.PublishingDate != null && item.PublishingDate > since)
            .OrderBy(item => item.PublishingDate)
            .Select(item =>
                new Post(
                    item.PublishingDate!.Value,
                    item.Title ?? string.Empty,
                    item.Description ?? string.Empty,
                    item.Categories?.ToList() ?? new List<string>(),
                    item.Author ?? string.Empty,
                    item.Link ?? string.Empty
                )
            )
            .ToList();

        // Todo - batching
        return posts;
    }
}
