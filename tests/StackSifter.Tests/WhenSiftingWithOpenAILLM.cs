using StackSifter.Feed;

namespace StackSifter.Tests;

public class WhenSiftingWithOpenAILLM
{
    [Test]
    public async Task ReturnsOnlyPostsMatchingLLMResponse()
    {
        // Arrange
        var posts = new List<Post>
        {
            new Post(DateTime.UtcNow, "How to use OpenAI API?", "Details...", new List<string>{"openai"}),
            new Post(DateTime.UtcNow, "Unrelated question", "Details...", new List<string>{"other"})
        };
        var llmSifter = new OpenAILLMSifter("fake-api-key", prompt: "Return only OpenAI questions");
        llmSifter.SetFilteredTitlesForTest(new List<string> { "How to use OpenAI API?" });

        // Act
        var filtered = new List<Post>();
        foreach (var post in posts)
        {
            if (await llmSifter.IsMatch(post))
                filtered.Add(post);
        }

        // Assert
        Assert.That(filtered, Has.Count.EqualTo(1));
        Assert.That(filtered[0].Title, Does.Contain("OpenAI"));
    }
}
