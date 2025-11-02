using System.Net;

namespace StackSifter.Tests.Utils;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseContent;
    private readonly HttpStatusCode _statusCode;
    private readonly Dictionary<string, string>? _urlToResponseMap;

    public MockHttpMessageHandler(string resourceOrXml, bool isRawXml = false, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _statusCode = statusCode;
        if (isRawXml)
        {
            _responseContent = resourceOrXml;
        }
        else
        {
            var assembly = typeof(MockHttpMessageHandler).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceOrXml);
            if (stream == null)
                throw new InvalidOperationException($"Resource not found: {resourceOrXml}");
            using var reader = new StreamReader(stream);
            _responseContent = reader.ReadToEnd();
        }
    }

    public MockHttpMessageHandler(Dictionary<string, string> urlToResponseMap, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _statusCode = statusCode;
        _urlToResponseMap = urlToResponseMap;
        _responseContent = string.Empty;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var content = _responseContent;
        
        // If we have a URL-based mapping, use it
        if (_urlToResponseMap != null && request.RequestUri != null)
        {
            var url = request.RequestUri.ToString();
            if (_urlToResponseMap.TryGetValue(url, out var mappedContent))
            {
                content = mappedContent;
            }
            else
            {
                // Try to match by URL pattern (e.g., if URL contains "openai", return OpenAI response)
                if (url.Contains("openai"))
                {
                    content = _urlToResponseMap.Values.FirstOrDefault(v => v.Contains("choices")) ?? content;
                }
                else
                {
                    content = _urlToResponseMap.Values.FirstOrDefault(v => !v.Contains("choices")) ?? content;
                }
            }
        }

        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(content)
        });
    }
}
