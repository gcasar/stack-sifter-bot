using System.Net;

namespace StackSifter.Tests.Utils;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseContent;
    public MockHttpMessageHandler(string resourceOrXml, bool isRawXml = false)
    {
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
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_responseContent)
        });
    }
}
