using Microsoft.Extensions.DependencyInjection;
using StackSifter.Configuration;

namespace StackSifter.Tests;

/// <summary>
/// Integration smoke test that validates end-to-end functionality using real API calls.
/// This test requires OPENAI_API_KEY environment variable and makes actual HTTP requests.
/// </summary>
[TestFixture]
[Explicit("Requires OPENAI_API_KEY environment variable and makes real API calls")]
public class IntegrationSmokeTest
{
    [Test]
    public async Task EndToEndSmokeTest_FetchesAndSiftsPostsFromLastDay()
    {
        // Arrange - Check for API key
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Ignore("OPENAI_API_KEY environment variable not set. Skipping integration test.");
            return;
        }

        // Create a minimal test configuration
        var config = ConfigurationLoader.LoadFromYaml(@"
feeds:
  - https://stackoverflow.com/feeds/tag?tagnames=c%23&sort=newest
rules:
  - prompt: 'Does this mention async/await or Task-related issues?'
    notify:
      - slack: '#test-channel'
");

        // Setup real HTTP client factory
        var services = new ServiceCollection();
        services.AddHttpClient();
        using var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        var service = new ConfigurableStackSifterService(config, apiKey, httpClientFactory);

        // Act - Fetch posts from the last 24 hours
        var since = DateTime.UtcNow.AddHours(-24);
        ProcessingResult? result = null;

        try
        {
            result = await service.ProcessAsync(since);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Integration test failed with exception: {ex.Message}\n{ex.StackTrace}");
        }

        // Assert - Verify the service executed without errors
        Assert.That(result, Is.Not.Null, "Service should return a result");
        Assert.That(result.TotalProcessed, Is.GreaterThanOrEqualTo(0), "Should process zero or more posts");
        Assert.That(result.Matches, Is.Not.Null, "Matches collection should not be null");

        // Log results for visibility
        TestContext.WriteLine($"Integration Test Results:");
        TestContext.WriteLine($"  Total Posts Processed: {result.TotalProcessed}");
        TestContext.WriteLine($"  Matching Posts Found: {result.Matches.Count}");
        TestContext.WriteLine($"  Last Created: {result.LastCreated?.ToString() ?? "None"}");

        if (result.Matches.Any())
        {
            TestContext.WriteLine($"\nMatched Posts:");
            foreach (var match in result.Matches.Take(3))
            {
                TestContext.WriteLine($"  - {match.Post.Title}");
                TestContext.WriteLine($"    Reason: {match.MatchReason}");
            }
        }

        // Basic sanity checks
        if (result.TotalProcessed > 0)
        {
            Assert.That(result.LastCreated, Is.Not.Null,
                "LastCreated should be set when posts are processed");
            Assert.That(result.LastCreated!.Value, Is.GreaterThan(since),
                "LastCreated should be after the 'since' timestamp");
        }
    }

    [Test]
    public async Task SmokeTest_ValidateConfigurationAndHttpClient()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Ignore("OPENAI_API_KEY environment variable not set. Skipping integration test.");
            return;
        }

        // Minimal config
        var config = ConfigurationLoader.LoadFromYaml(@"
feeds:
  - https://stackoverflow.com/feeds/tag?tagnames=csharp&sort=newest
rules:
  - prompt: 'Test prompt'
    notify:
      - slack: '#test'
");

        var services = new ServiceCollection();
        services.AddHttpClient();
        using var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Act - Just verify we can create the service without errors
        ConfigurableStackSifterService? service = null;
        Assert.DoesNotThrow(() =>
        {
            service = new ConfigurableStackSifterService(config, apiKey, httpClientFactory);
        }, "Should be able to create service with valid configuration");

        Assert.That(service, Is.Not.Null, "Service should be instantiated successfully");

        // Verify we can make a simple call (with very recent timestamp to minimize data)
        var veryRecent = DateTime.UtcNow.AddMinutes(-5);
        ProcessingResult? result = null;

        Assert.DoesNotThrowAsync(async () =>
        {
            result = await service.ProcessAsync(veryRecent);
        }, "ProcessAsync should complete without throwing exceptions");

        Assert.That(result, Is.Not.Null, "Should return a valid result");
        TestContext.WriteLine($"Smoke test processed {result.TotalProcessed} posts from last 5 minutes");
    }
}
