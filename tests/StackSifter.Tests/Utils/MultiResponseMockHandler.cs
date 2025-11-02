using System.Net;

namespace StackSifter.Tests.Utils;

/// <summary>
/// A mock HTTP handler that returns different responses based on whether the URL contains "openai".
/// Used for testing the full flow where both feed fetching and LLM API calls are made.
/// </summary>
public class MultiResponseMockHandler : HttpMessageHandler
{
    private readonly string _feedResponse;
    private readonly string _openAiResponse;
    private readonly HttpStatusCode _statusCode;

    public MultiResponseMockHandler(string feedResponse, string openAiResponse, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _statusCode = statusCode;

        // Load feed response
        var assembly = typeof(MockHttpMessageHandler).Assembly;
        using var feedStream = assembly.GetManifestResourceStream(feedResponse);
        if (feedStream == null)
            throw new InvalidOperationException($"Resource not found: {feedResponse}");
        using var feedReader = new StreamReader(feedStream);
        _feedResponse = feedReader.ReadToEnd();

        // Load OpenAI response
        using var openAiStream = assembly.GetManifestResourceStream(openAiResponse);
        if (openAiStream == null)
            throw new InvalidOperationException($"Resource not found: {openAiResponse}");
        using var openAiReader = new StreamReader(openAiStream);
        _openAiResponse = openAiReader.ReadToEnd();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString() ?? string.Empty;
        var content = url.Contains("openai", StringComparison.OrdinalIgnoreCase) 
            ? _openAiResponse 
            : _feedResponse;

        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(content)
        });
    }
}
