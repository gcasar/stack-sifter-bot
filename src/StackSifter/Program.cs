using StackSifter;
using StackSifter.Configuration;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

// Require OpenAI API key from environment variable
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

// Parse arguments
if (args.Length < 2)
{
    PrintUsage();
    return 1;
}

var configPath = args[0];
if (!DateTime.TryParse(args[1], null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var since))
{
    Console.Error.WriteLine($"Invalid timestamp: {args[1]}");
    PrintUsage();
    return 1;
}

try
{
    Console.Error.WriteLine($"Loading configuration from: {configPath}");
    var config = ConfigurationLoader.LoadFromFile(configPath);

    Console.Error.WriteLine($"Processing {config.Feeds.Count} feeds with {config.Rules.Count} rules...");

    // Setup dependency injection
    var services = new ServiceCollection();
    services.AddHttpClient();
    var serviceProvider = services.BuildServiceProvider();
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

    var service = new ConfigurableStackSifterService(config, apiKey, httpClientFactory);
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
            MatchReason = m.MatchReason
        }).ToList()
    };

    var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(json);

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

static void PrintUsage()
{
    Console.WriteLine("Usage: stack-sifter <config.yaml> <timestamp>");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  <config.yaml>   Path to YAML configuration file");
    Console.WriteLine("  <timestamp>     UTC timestamp (e.g., 2025-07-05T12:34:56Z)");
    Console.WriteLine();
    Console.WriteLine("Environment variables:");
    Console.WriteLine("  OPENAI_API_KEY  Required - OpenAI API key for LLM sifting");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  stack-sifter stack-sifter.yaml 2025-07-05T12:00:00Z");
}
