using StackSifter.Feed;
using StackSifter;

// Require UTC timestamp as argument
if (args.Length == 0 || !DateTime.TryParse(args[0], null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var since))
{
    Console.WriteLine("Please provide a UTC timestamp as the first argument (e.g., 2025-07-05T12:34:56Z)");
    return;
}
Console.WriteLine($"Last run timestamp (UTC): {since:O}");

// Wire things up!
IPostsFeed feed = new StackOverflowRSSFeed();
var sifter = new AllMatchPostSifter();
var service = new PostsProcessingService(feed, sifter);

// Run the actual processing
var posts = await service.FetchAndFilterPostsAsync(since);

// For now we simply echo the matches - and we only support a single matcher
foreach (var post in posts)
{
    Console.WriteLine($"Created: [{post.Published:O}]\n Title: {post.Title}\nTags: {string.Join(", ", post.Tags)}\n");
}
