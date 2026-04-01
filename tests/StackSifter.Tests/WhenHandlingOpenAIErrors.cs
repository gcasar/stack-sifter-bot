using StackSifter.Feed;
using StackSifter.Tests.Utils;
using System.Net;
using System.Text.Json;

namespace StackSifter.Tests;

[TestFixture]
public class WhenHandlingOpenAIErrors
{
    [Test]
    public void ThrowsWhenOpenAIReturnsError()
    {
        // Arrange
        var post = new Post(DateTime.UtcNow, "Test Post", "Details...", new List<string>(), "Author", "https://example.com");
        var handler = new MockHttpMessageHandler("error", isRawXml: true, statusCode: HttpStatusCode.BadRequest);
        var httpClientFactory = new MockHttpClientFactory(handler);
        var sifter = new OpenAILLMSifter("fake-key", "Test prompt", httpClientFactory);

        // Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(async () => await sifter.IsMatch(post));
    }

    [Test]
    public void ThrowsWhenOpenAIReturnsInvalidJson()
    {
        // Arrange
        var post = new Post(DateTime.UtcNow, "Test Post", "Details...", new List<string>(), "Author", "https://example.com");
        var handler = new MockHttpMessageHandler("not valid json", isRawXml: true);
        var httpClientFactory = new MockHttpClientFactory(handler);
        var sifter = new OpenAILLMSifter("fake-key", "Test prompt", httpClientFactory);

        // Act & Assert - should throw when parsing invalid JSON
        var ex = Assert.CatchAsync(async () => await sifter.IsMatch(post));
        Assert.That(ex, Is.InstanceOf<JsonException>());
    }

    [Test]
    public void ThrowsWhenOpenAIResponseMissingExpectedFields()
    {
        // Arrange
        var post = new Post(DateTime.UtcNow, "Test Post", "Details...", new List<string>(), "Author", "https://example.com");
        var invalidResponse = "{\"choices\":[]}"; // Missing message content
        var handler = new MockHttpMessageHandler(invalidResponse, isRawXml: true);
        var httpClientFactory = new MockHttpClientFactory(handler);
        var sifter = new OpenAILLMSifter("fake-key", "Test prompt", httpClientFactory);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await sifter.IsMatch(post));
        Assert.That(ex!.Message, Does.Contain("response"));
    }
}
