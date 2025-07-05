using StackSifter.Feed;
// Require UTC timestamp as argument
if (args.Length == 0 || !DateTime.TryParse(args[0], null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var since))
{
    Console.WriteLine("Please provide a UTC timestamp as the first argument (e.g., 2025-07-05T12:34:56Z)");
    return;
}
Console.WriteLine($"Last run timestamp (UTC): {since:O}");

// Configure IPostsFeed to use StackOverflowRSSFeed
IPostsFeed feed = new StackOverflowRSSFeed();
var posts = await feed.FetchPostsSinceAsync(since);

foreach (var post in posts)
{
    Console.WriteLine($"[{post.Published:O}] {post.Title}\nTags: {string.Join(", ", post.Tags)}\n");
}
