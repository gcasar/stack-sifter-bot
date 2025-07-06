using Moq;
using StackSifter.Feed;

namespace StackSifter.Tests;

public class WhenProcessingPosts
{
    [Test]
    public async Task OnlyMatchingPostsAreReturned()
    {
        // Arrange
        var posts = new List<Post>
        {
            new(DateTime.UtcNow.AddMinutes(-10), "A", "Brief A", new() { "c#" }, "AuthorA", "https://example.com/a"),
            new(DateTime.UtcNow.AddMinutes(-5), "B", "Brief B", new() { "java" }, "AuthorB", "https://example.com/b")
        };
        var feedMock = new Mock<IPostsFeed>();
        feedMock.Setup(f => f.FetchPostsSinceAsync(It.IsAny<DateTime>())).ReturnsAsync(posts);
        var sifterMock = new Mock<IPostSifter>();
        sifterMock.Setup(s => s.IsMatch(It.Is<Post>(p => p.Title == "A"))).ReturnsAsync(true);
        sifterMock.Setup(s => s.IsMatch(It.Is<Post>(p => p.Title == "B"))).ReturnsAsync(false);
        var service = new PostsProcessingService(feedMock.Object, sifterMock.Object);

        // Act
        var result = await service.FetchAndFilterPostsAsync(DateTime.UtcNow.AddHours(-1));

        // Assert
        Assert.That(result, Has.Exactly(1).Items);
        Assert.That(result[0].Title, Is.EqualTo("A"));
    }
}
