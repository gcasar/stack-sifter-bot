using StackSifter.Feed;
using StackSifter;
using System.Text.Json;

// Require UTC timestamp as argument
if (args.Length == 0 || !DateTime.TryParse(args[0], null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var since))
{
    Console.WriteLine("Please provide a UTC timestamp as the first argument (e.g., 2025-07-05T12:34:56Z)");
    return;
}
// Optionally accept feedUrl as second argument
string? feedUrl = args.Length > 1 ? args[1] : null;

// Wire things up!
IPostsFeed feed = new StackOverflowRSSFeed(feedUrl: feedUrl);
var sifter = new AllMatchPostSifter();
var service = new PostsProcessingService(feed, sifter);

// Run the actual processing
var posts = await service.FetchAndFilterPostsAsync(since);
var minimalPosts = posts.Select( p => new { Created=p.Published, p.Title, p.Tags }).ToList();

var json = JsonSerializer.Serialize(minimalPosts, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);
