using System.Net;

namespace StackSifter.Tests.Utils;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseContent;
    public MockHttpMessageHandler(string resourceName)
    {
        var assembly = typeof(MockHttpMessageHandler).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException($"Resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        _responseContent = reader.ReadToEnd();
    }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_responseContent)
        });
    }
}
