using StackSifter.Feed;
using StackSifter;
using StackSifter.Configuration;
using System.Text.Json;

// Parse command-line arguments
var args_parsed = ParseArguments(args);
if (args_parsed == null)
{
    PrintUsage();
    return 1;
}

// Require OpenAI API key from environment variable
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

try
{
    if (args_parsed.Value.UseConfig)
    {
        // Mode 1: Use YAML configuration file
        await RunWithConfigAsync(args_parsed.Value.ConfigPath!, args_parsed.Value.Since, apiKey);
    }
    else
    {
        // Mode 2: Legacy mode with CLI arguments
        await RunLegacyModeAsync(args_parsed.Value.Since, args_parsed.Value.FeedUrl, apiKey);
    }

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.Error.WriteLine($"  Inner: {ex.InnerException.Message}");
    }
    return 1;
}

/// <summary>
/// Runs the stack sifter using a YAML configuration file.
/// </summary>
static async Task RunWithConfigAsync(string configPath, DateTime since, string apiKey)
{
    Console.Error.WriteLine($"Loading configuration from: {configPath}");
    var config = ConfigurationLoader.LoadFromFile(configPath);

    Console.Error.WriteLine($"Processing {config.Feeds.Count} feeds with {config.Rules.Count} rules...");

    var service = new ConfigurableStackSifterService(config, apiKey);
    var result = await service.ProcessAsync(since);

    // Output results as JSON
    var output = new
    {
        TotalProcessed = result.TotalProcessed,
        LastCreated = result.LastCreated,
        MatchingPosts = result.Matches.Select(m => new
        {
            Created = m.Post.Published,
            m.Post.Title,
            m.Post.Tags,
            m.Post.Url,
            MatchReason = m.MatchReason,
            NotificationTargets = m.NotificationTargets.Select(t => t.GetDescription()).ToList()
        }).ToList()
    };

    var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);
}

/// <summary>
/// Runs in legacy mode with hardcoded criteria (backward compatibility).
/// </summary>
static async Task RunLegacyModeAsync(DateTime since, string? feedUrl, string apiKey)
{
    Console.Error.WriteLine("Running in legacy mode (consider migrating to YAML config)...");

    IPostsFeed feed = new StackOverflowRSSFeed(feedUrl: feedUrl);
    var criteriaPrompt = "Does this post contain a question about Python or C code?";
    var sifter = new OpenAILLMSifter(apiKey, criteriaPrompt);
    var service = new PostsProcessingService(feed, sifter);

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
}

/// <summary>
/// Parses and validates command-line arguments.
/// </summary>
static (bool UseConfig, string? ConfigPath, DateTime Since, string? FeedUrl)? ParseArguments(string[] args)
{
    if (args.Length == 0)
        return null;

    // Check if first argument is a config file flag
    if (args[0] == "--config" || args[0] == "-c")
    {
        if (args.Length < 3)
            return null;

        var configPath = args[1];
        if (!DateTime.TryParse(args[2], null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var since))
            return null;

        return (true, configPath, since, null);
    }
    else
    {
        // Legacy mode: first arg is timestamp, optional second is feed URL
        if (!DateTime.TryParse(args[0], null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var since))
            return null;

        string? feedUrl = args.Length > 1 ? args[1] : null;
        return (false, null, since, feedUrl);
    }
}

/// <summary>
/// Prints usage information.
/// </summary>
static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  # Using configuration file:");
    Console.WriteLine("  stack-sifter --config <config.yaml> <timestamp>");
    Console.WriteLine();
    Console.WriteLine("  # Legacy mode (hardcoded criteria):");
    Console.WriteLine("  stack-sifter <timestamp> [feed-url]");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  <timestamp>     UTC timestamp (e.g., 2025-07-05T12:34:56Z)");
    Console.WriteLine("  <config.yaml>   Path to YAML configuration file");
    Console.WriteLine("  [feed-url]      Optional RSS feed URL (legacy mode only)");
    Console.WriteLine();
    Console.WriteLine("Environment variables:");
    Console.WriteLine("  OPENAI_API_KEY  Required - OpenAI API key for LLM sifting");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  stack-sifter --config stack-sifter.yaml 2025-07-05T12:00:00Z");
    Console.WriteLine("  stack-sifter 2025-07-05T12:00:00Z");
    Console.WriteLine("  stack-sifter 2025-07-05T12:00:00Z https://meta.stackoverflow.com/feeds");
}
