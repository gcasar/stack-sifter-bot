using StackSifter.Feed;
using StackSifter;
using System.Text.Json;

// Require OpenAI API key from environment variable
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

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
// Configure OpenAILLMSifter to filter for Python or C code questions
var criteriaPrompt = "Does this post contain a question about Python or C code?";
var sifter = new OpenAILLMSifter(apiKey, criteriaPrompt);
var service = new PostsProcessingService(feed, sifter);

// Run the actual processing
var posts = await service.FetchAndFilterPostsAsync(since);
var minimalPosts = posts.Select(p => new { Created = p.Published, p.Title, p.Tags, p.Url }).ToList();

var metadata = new
{
    TotalProcessed = posts.Count,
    LastCreated = posts.Count > 0 ? posts.Max(p => p.Published) : (DateTime?)null,
    MatchingPosts = minimalPosts
};

var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);
