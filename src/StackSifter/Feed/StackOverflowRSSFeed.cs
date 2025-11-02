using CodeHollow.FeedReader;
using System.Net;
using System.Net.Http;

namespace StackSifter.Feed;

/// <summary>
/// Fetches and parses posts from Stack Overflow RSS feeds.
/// </summary>
public class StackOverflowRSSFeed : IPostsFeed
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _feedUrl;

    /// <summary>
    /// Initializes a new instance of the StackOverflowRSSFeed class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="feedUrl">The RSS feed URL to fetch. Defaults to the main Stack Overflow feed.</param>
    public StackOverflowRSSFeed(IHttpClientFactory httpClientFactory, string? feedUrl = null)
    {
        _httpClientFactory = httpClientFactory;
        _feedUrl = feedUrl ?? "https://stackoverflow.com/feeds";
    }

    /// <summary>
    /// Fetches posts from the RSS feed that were published after the specified timestamp.
    /// </summary>
    /// <param name="since">Only return posts published after this timestamp.</param>
    /// <returns>List of posts published after the specified timestamp, ordered by publishing date.</returns>
    public async Task<List<Post>> FetchPostsSinceAsync(DateTime since)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("StackSifterBot/1.0 (+https://github.com/gcasar/stack-sifter)");
        var xml = await httpClient.GetStringAsync(_feedUrl);
        // Replace problematic entities (e.g., &bull;) with safe equivalents
        xml = xml.Replace("&bull;", "•");
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
