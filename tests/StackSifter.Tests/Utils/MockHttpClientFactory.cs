namespace StackSifter.Tests.Utils;

public class MockHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public MockHttpClientFactory(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(_handler);
    }
}
