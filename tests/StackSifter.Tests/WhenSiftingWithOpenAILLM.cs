
using DotEnv = dotenv.net.DotEnv;
using StackSifter.Feed;

namespace StackSifter.Tests;

public class WhenSiftingWithOpenAILLM
{
    [OneTimeSetUp]
    public void LoadEnv()
    {
        DotEnv.Load(); // Loads .env file if present
    }
    [Test]
    public async Task ReturnsOnlyPostsMatchingLLMResponse()
    {
        // Arrange
        var posts = new List<Post>
        {
            new Post(DateTime.UtcNow, "How to use OpenAI API?", "Details...", new List<string>{"openai"}),
            new Post(DateTime.UtcNow, "Unrelated question", "Details...", new List<string>{"other"})
        };
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        Assert.That(apiKey, Is.Not.Null.And.Not.Empty, "OPENAI_API_KEY environment variable must be set for integration test.");
        var llmSifter = new OpenAILLMSifter(apiKey, prompt: "Return 'yes' if the post is about OpenAI, otherwise 'no'. Only answer 'yes' or 'no'.");

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
