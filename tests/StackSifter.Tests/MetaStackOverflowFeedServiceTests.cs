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
}
