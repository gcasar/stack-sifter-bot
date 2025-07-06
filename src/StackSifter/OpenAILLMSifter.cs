namespace StackSifter;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using StackSifter.Feed;

public class OpenAILLMSifter : IPostSifter
{
    private readonly string _apiKey;
    private readonly string _prompt;
    private readonly HttpClient _httpClient;


    private List<string>? _lastFilteredTitles;
    private List<Post>? _lastPosts;
    private bool _llmCalled;

    public OpenAILLMSifter(string apiKey, string prompt, HttpClient? httpClient = null)
    {
        _apiKey = apiKey;
        _prompt = prompt;
        _httpClient = httpClient ?? new HttpClient();
        _llmCalled = false;
    }

    // For testability, allow setting the filtered titles directly (mocking LLM)
    public void SetFilteredTitlesForTest(List<string> titles)
    {
        _lastFilteredTitles = titles;
        _llmCalled = true;
    }

    public bool IsMatch(Post post)
    {
        // For test, if SetFilteredTitlesForTest was called, use that
        if (_llmCalled && _lastFilteredTitles != null)
            return _lastFilteredTitles.Contains(post.Title);
        // In production, you would want to call the LLM here with the batch of posts
        // For now, always return false to avoid network calls in tests
        return false;
    }
}
