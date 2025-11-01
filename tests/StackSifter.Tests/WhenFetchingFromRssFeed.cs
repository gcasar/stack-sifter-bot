using StackSifter.Feed;
using StackSifter.Tests.Utils;

namespace StackSifter.Tests;

public class WhenFetchingFromRssFeed
{
    [Test]
    public async Task MapsAllPostFields()
    {
        // Arrange
        var lastRun = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var resourceName = "StackSifter.Tests.TestData.meta_stackoverflow_feed.xml";
        var handler = new MockHttpMessageHandler(resourceName);
        var httpClientFactory = new MockHttpClientFactory(handler);
        var service = new StackOverflowRSSFeed(httpClientFactory);

        // Act
        var posts = await service.FetchPostsSinceAsync(lastRun);

        // Assert
        Assert.That(posts, Is.Not.Empty);
        foreach (var post in posts)
        {
            Assert.That(post.Title, Is.Not.Null.And.Not.Empty, "Title should be mapped");
            Assert.That(post.Brief, Is.Not.Null, "Brief should be mapped");
            Assert.That(post.Tags, Is.Not.Null, "Tags should be mapped");
        }
    }

    [Test]
    public async Task ReturnsEmptyListIfNoPostsAfterSince()
    {
        // Arrange: set since to a future date
        var lastRun = new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var resourceName = "StackSifter.Tests.TestData.meta_stackoverflow_feed.xml";
        var handler = new MockHttpMessageHandler(resourceName);
        var httpClientFactory = new MockHttpClientFactory(handler);
        var service = new StackOverflowRSSFeed(httpClientFactory);

        // Act
        var posts = await service.FetchPostsSinceAsync(lastRun);

        // Assert
        Assert.That(posts, Is.Empty, "Should return empty list if no posts are newer than 'since'");
    }

    [Test]
    public async Task HandlesFeedWithNoEntries()
    {
        // Arrange: use a feed with no <entry>
        var emptyFeed = "<?xml version=\"1.0\"?><feed xmlns=\"http://www.w3.org/2005/Atom\"></feed>";
        var handler = new MockHttpMessageHandler(emptyFeed, isRawXml: true);
        var httpClientFactory = new MockHttpClientFactory(handler);
        var service = new StackOverflowRSSFeed(httpClientFactory);

        // Act
        var posts = await service.FetchPostsSinceAsync(DateTime.MinValue);

        // Assert
        Assert.That(posts, Is.Empty, "Should return empty list for feed with no entries");
    }

    [Test]
    public async Task HandlesMalformedEntitiesGracefully()
    {
        // Arrange: feed with &bull; entity
        var xml = "<?xml version=\"1.0\"?><feed xmlns=\"http://www.w3.org/2005/Atom\"><entry><title>Test &bull; Post</title><published>2024-07-02T12:00:00Z</published></entry></feed>";
        var handler = new MockHttpMessageHandler(xml, isRawXml: true);
        var httpClientFactory = new MockHttpClientFactory(handler);
        var service = new StackOverflowRSSFeed(httpClientFactory);

        // Act
        var posts = await service.FetchPostsSinceAsync(DateTime.MinValue);

        // Assert
        Assert.That(posts[0].Title, Does.Contain("•"), "Should replace &bull; with bullet");
    }
}
