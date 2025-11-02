using StackSifter.Configuration;

namespace StackSifter.Tests;

[TestFixture]
public class WhenValidatingConfiguration
{
    [Test]
    public void RejectsInvalidFeedUrls()
    {
        var yaml = @"
feeds:
  - not-a-valid-url
rules:
  - prompt: 'Test'
    notify:
      - slack: '#test'
";
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigurationLoader.LoadFromYaml(yaml));
        Assert.That(ex!.Message, Does.Contain("feed").IgnoreCase);
        Assert.That(ex!.Message, Does.Contain("URL").IgnoreCase);
    }

    [Test]
    public void AcceptsValidHttpsUrls()
    {
        var yaml = @"
feeds:
  - https://stackoverflow.com/feeds
rules:
  - prompt: 'Test'
    notify:
      - slack: '#test'
";
        Assert.DoesNotThrow(() => ConfigurationLoader.LoadFromYaml(yaml));
    }

    [Test]
    public void AcceptsValidHttpUrls()
    {
        var yaml = @"
feeds:
  - http://example.com/feed
rules:
  - prompt: 'Test'
    notify:
      - slack: '#test'
";
        Assert.DoesNotThrow(() => ConfigurationLoader.LoadFromYaml(yaml));
    }
}
