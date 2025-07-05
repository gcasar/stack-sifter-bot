using StackSifter.Feed;
using System.Net;

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
        var httpClient = new HttpClient(handler);
        var service = new StackOverflowRSSFeed(httpClient);

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

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseContent;
        public MockHttpMessageHandler(string resourceName)
        {
            var assembly = typeof(WhenFetchingFromRssFeed).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Resource not found: {resourceName}");
            using var reader = new StreamReader(stream);
            _responseContent = reader.ReadToEnd();
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseContent)
            });
        }
    }
}
