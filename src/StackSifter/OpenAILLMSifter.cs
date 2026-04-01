using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using StackSifter.Feed;

namespace StackSifter;

/// <summary>
/// An LLM-based post sifter that uses OpenAI's API to evaluate posts against criteria.
/// </summary>
public class OpenAILLMSifter : IPostSifter
{
    private readonly string _apiKey;
    private readonly string _criteria;
    private readonly IHttpClientFactory _httpClientFactory;

    private const string OpenAIEndpoint = "https://api.openai.com/v1/chat/completions";
    private const string DefaultModel = "gpt-4o-mini";
    private const int MaxTokens = 5;
    private const double Temperature = 0.0;
    private const int RequestCount = 1;
    private const string StopSequence = "\n";
    private const string SystemPromptTemplate = "You are an AI assistant that answers only with 'yes' or 'no'. Evaluate if the post matches the following criteria: {0} Answer only 'yes' or 'no'.";

    /// <summary>
    /// Initializes a new instance of the OpenAILLMSifter class.
    /// </summary>
    /// <param name="apiKey">The OpenAI API key for authentication.</param>
    /// <param name="criteriaPrompt">The criteria prompt to evaluate posts against.</param>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    public OpenAILLMSifter(string apiKey, string criteriaPrompt, IHttpClientFactory httpClientFactory)
    {
        _apiKey = apiKey;
        _criteria = criteriaPrompt;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Evaluates whether a post matches the configured criteria using OpenAI's LLM.
    /// </summary>
    /// <param name="post">The post to evaluate.</param>
    /// <returns>True if the post matches the criteria, false otherwise.</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    /// <exception cref="JsonException">Thrown when the API response cannot be parsed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the API response is missing expected fields.</exception>
    public async Task<bool> IsMatch(Post post)
    {
        // Prepare the prompt for the LLM
        var systemPrompt = string.Format(SystemPromptTemplate, _criteria);
        var requestBody = new
        {
            model = DefaultModel,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Title: {post.Title}\nBrief: {post.Brief}" }
            },
            max_tokens = MaxTokens,
            temperature = Temperature,
            n = RequestCount,
            stop = StopSequence
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await httpClient.PostAsync(OpenAIEndpoint, content);
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseString);
        var answer = ExtractAnswerFromResponse(doc);

        // Interpret the answer as a yes/no
        return answer != null && answer.Trim().ToLower().StartsWith("yes");
    }

    /// <summary>
    /// Extracts the answer content from an OpenAI API response.
    /// </summary>
    /// <param name="doc">The parsed JSON document.</param>
    /// <returns>The answer content string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the response is missing expected fields.</exception>
    private static string? ExtractAnswerFromResponse(JsonDocument doc)
    {
        var root = doc.RootElement;

        // Validate response structure
        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("OpenAI response missing choices array or array is empty");
        }

        var firstChoice = choices[0];
        if (!firstChoice.TryGetProperty("message", out var message) ||
            !message.TryGetProperty("content", out var contentElement))
        {
            throw new InvalidOperationException("OpenAI response missing message content");
        }

        return contentElement.GetString();
    }
}
