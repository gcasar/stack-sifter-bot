using StackSifter.Configuration;
using StackSifter.Feed;
using StackSifter.Tests.Utils;

namespace StackSifter.Tests;

[TestFixture]
public class WhenProcessingWithConfigurableService
{
    [Test]
    public async Task ProcessesPostsFromMultipleFeeds()
    {
        // Arrange
        var config = new StackSifterConfig
        {
            Feeds = new List<string> { "https://feed1.com", "https://feed2.com" },
            Rules = new List<SiftingRule>
            {
                new SiftingRule
                {
                    Prompt = "Test prompt",
                    Notify = new List<NotificationTarget> { new NotificationTarget { Slack = "#test" } }
                }
            }
        };

        var handler = new MultiResponseMockHandler(
            feedResponse: "StackSifter.Tests.TestData.meta_stackoverflow_feed.xml",
            openAiResponse: "StackSifter.Tests.TestData.openai_response_no_snapshot.json"
        );
        var httpClientFactory = new MockHttpClientFactory(handler);

        var service = new ConfigurableStackSifterService(config, "fake-api-key", httpClientFactory);
        var since = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await service.ProcessAsync(since);

        // Assert
        Assert.That(result.TotalProcessed, Is.GreaterThan(0));
    }

    [Test]
    public async Task ReturnsEmptyMatchesWhenNoPostsMatchRules()
    {
        // Arrange
        var config = new StackSifterConfig
        {
            Feeds = new List<string> { "https://feed1.com" },
            Rules = new List<SiftingRule>
            {
                new SiftingRule
                {
                    Prompt = "Test prompt",
                    Notify = new List<NotificationTarget> { new NotificationTarget { Slack = "#test" } }
                }
            }
        };

        var handler = new MultiResponseMockHandler(
            feedResponse: "StackSifter.Tests.TestData.meta_stackoverflow_feed.xml",
            openAiResponse: "StackSifter.Tests.TestData.openai_response_no_snapshot.json"
        );
        var httpClientFactory = new MockHttpClientFactory(handler);

        var service = new ConfigurableStackSifterService(config, "fake-api-key", httpClientFactory);
        var since = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await service.ProcessAsync(since);

        // Assert
        Assert.That(result.Matches, Is.Empty);
    }

    [Test]
    public async Task TracksLastCreatedTimestamp()
    {
        // Arrange
        var config = new StackSifterConfig
        {
            Feeds = new List<string> { "https://feed1.com" },
            Rules = new List<SiftingRule>
            {
                new SiftingRule
                {
                    Prompt = "Test",
                    Notify = new List<NotificationTarget> { new NotificationTarget { Slack = "#test" } }
                }
            }
        };

        var handler = new MultiResponseMockHandler(
            feedResponse: "StackSifter.Tests.TestData.meta_stackoverflow_feed.xml",
            openAiResponse: "StackSifter.Tests.TestData.openai_response_no_snapshot.json"
        );
        var httpClientFactory = new MockHttpClientFactory(handler);

        var service = new ConfigurableStackSifterService(config, "fake-api-key", httpClientFactory);
        var since = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await service.ProcessAsync(since);

        // Assert
        Assert.That(result.LastCreated, Is.Not.Null);
        Assert.That(result.LastCreated, Is.GreaterThan(since));
    }
}
