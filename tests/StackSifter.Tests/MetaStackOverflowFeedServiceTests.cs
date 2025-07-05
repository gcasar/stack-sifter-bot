using StackSifter.Feed;

namespace StackSifter.Tests;

public class MetaStackOverflowFeedServiceTests
{
    [Test]
    public async Task FetchPostsSince_ReturnsBatchesOf10()
    {
        // Arrange
        var lastRun = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var service = new MetaStackOverflowFeedService();

        // Act
        var posts = await service.FetchPostsSinceAsync(lastRun);

        // Assert
        Assert.That(posts, Is.Not.Null);
        Assert.That(posts.Count % 10, Is.EqualTo(0).Or.EqualTo(posts.Count));
        // Optionally: check that all posts are newer than lastRun
        Assert.That(posts, Is.All.Matches<Post>(p => p.Published > lastRun));
    }

    [Test]
    public async Task FetchPostsSince_MapsAllPostFields()
    {
        // Arrange
        var lastRun = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var service = new MetaStackOverflowFeedService();

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
}
