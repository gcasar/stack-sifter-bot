namespace StackSifter;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using StackSifter.Feed;

public class OpenAILLMSifter : IPostSifter
{
    private readonly string _apiKey;
    private readonly string _criteria;
    private readonly HttpClient _httpClient;

    private const string SystemPromptTemplate = "You are an AI assistant that answers only with 'yes' or 'no'. Use the following criteria: {0} Answer only 'yes' or 'no'.";

    private List<string>? _lastFilteredTitles;
    private List<Post>? _lastPosts;
    private bool _llmCalled;

    public OpenAILLMSifter(string apiKey, string criteriaPrompt, HttpClient? httpClient = null)
    {
        _apiKey = apiKey;
        _criteria = criteriaPrompt;
        _httpClient = httpClient ?? new HttpClient();
        _llmCalled = false;
    }

    // For testability, allow setting the filtered titles directly (mocking LLM)
    public void SetFilteredTitlesForTest(List<string> titles)
    {
        _lastFilteredTitles = titles;
        _llmCalled = true;
    }

    public async Task<bool> IsMatch(Post post)
    {
        // For test, if SetFilteredTitlesForTest was called, use that
        if (_llmCalled && _lastFilteredTitles != null)
            return _lastFilteredTitles.Contains(post.Title);

        // Prepare the prompt for the LLM
        var systemPrompt = string.Format(SystemPromptTemplate, _criteria);
        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Title: {post.Title}\nBrief: {post.Brief}" }
            },
            max_tokens = 1,
            temperature = 0.0,
            n = 1,
            stop = "\n"
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseString);
        var root = doc.RootElement;
        var answer = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        // Interpret the answer as a yes/no (customize as needed)
        return answer != null && answer.Trim().ToLower().StartsWith("yes");
    }
}
