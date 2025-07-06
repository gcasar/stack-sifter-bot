using StackSifter.Feed;
using StackSifter.Tests.Utils;

namespace StackSifter.Tests;

public class WhenSiftingWithOpenAILLM
{


    [Test]
    public async Task ReturnsMatchWhenLLMRespondsYes()
    {
        // Arrange
        var post = new Post(DateTime.UtcNow, "How to use OpenAI API?", "Details...", new List<string>{"openai"}, "TestAuthor", "https://example.com");
        var apiKey = "fake-api-key";
        var yesResponsePath = "StackSifter.Tests.TestData.openai_response_yes_snapshot.json";
        var handlerYes = new MockHttpMessageHandler(yesResponsePath);
        var httpClientYes = new HttpClient(handlerYes);
        var llmSifterYes = new OpenAILLMSifter(apiKey, "The post is about OpenAI", httpClientYes);

        // Act
        var isMatch = await llmSifterYes.IsMatch(post);

        // Assert
        Assert.That(isMatch, Is.True);
    }

    [Test]
    public async Task DoesNotMatchPostWhenLLMRespondsNo()
    {
        // Arrange
        var post = new Post(DateTime.UtcNow, "Unrelated question", "Details...", new List<string>{"other"}, "TestAuthor", "https://example.com");
        var apiKey = "fake-api-key";
        var noResponsePath = "StackSifter.Tests.TestData.openai_response_no_snapshot.json";
        var handlerNo = new MockHttpMessageHandler(noResponsePath);
        var httpClientNo = new HttpClient(handlerNo);
        var llmSifterNo = new OpenAILLMSifter(apiKey, "The post is about OpenAI", httpClientNo);

        // Act
        var isMatch = await llmSifterNo.IsMatch(post);

        // Assert
        Assert.That(isMatch, Is.False);
    }
}
