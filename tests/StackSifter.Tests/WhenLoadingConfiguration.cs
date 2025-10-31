using StackSifter.Configuration;

namespace StackSifter.Tests;

[TestFixture]
public class WhenLoadingConfiguration
{
    [Test]
    public void ValidConfiguration_ShouldLoadSuccessfully()
    {
        var yaml = @"
feeds:
  - https://stackoverflow.com/feeds/tag?tagnames=c%23&sort=newest
  - https://meta.stackoverflow.com/feeds
poll_interval_minutes: 5
rules:
  - prompt: 'Is this related to authentication?'
";

        var config = ConfigurationLoader.LoadFromYaml(yaml);

        Assert.That(config.Feeds, Has.Count.EqualTo(2));
        Assert.That(config.Feeds[0], Is.EqualTo("https://stackoverflow.com/feeds/tag?tagnames=c%23&sort=newest"));
        Assert.That(config.PollIntervalMinutes, Is.EqualTo(5));
        Assert.That(config.Rules, Has.Count.EqualTo(1));
        Assert.That(config.Rules[0].Prompt, Is.EqualTo("Is this related to authentication?"));
    }

    [Test]
    public void MultipleRules_ShouldLoadCorrectly()
    {
        var yaml = @"
feeds:
  - https://stackoverflow.com/feeds
rules:
  - prompt: 'First rule'
  - prompt: 'Second rule'
  - prompt: 'Third rule'
";

        var config = ConfigurationLoader.LoadFromYaml(yaml);

        Assert.That(config.Rules, Has.Count.EqualTo(3));
        Assert.That(config.Rules[0].Prompt, Is.EqualTo("First rule"));
        Assert.That(config.Rules[1].Prompt, Is.EqualTo("Second rule"));
        Assert.That(config.Rules[2].Prompt, Is.EqualTo("Third rule"));
    }

    [Test]
    public void NoFeeds_ShouldThrowValidationError()
    {
        var yaml = @"
feeds: []
rules:
  - prompt: 'Test'
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationLoader.LoadFromYaml(yaml));

        Assert.That(ex!.Message, Does.Contain("at least one feed"));
    }

    [Test]
    public void NoRules_ShouldThrowValidationError()
    {
        var yaml = @"
feeds:
  - https://stackoverflow.com/feeds
rules: []
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationLoader.LoadFromYaml(yaml));

        Assert.That(ex!.Message, Does.Contain("at least one sifting rule"));
    }

    [Test]
    public void EmptyPrompt_ShouldThrowValidationError()
    {
        var yaml = @"
feeds:
  - https://stackoverflow.com/feeds
rules:
  - prompt: ''
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationLoader.LoadFromYaml(yaml));

        Assert.That(ex!.Message, Does.Contain("non-empty prompt"));
    }

    [Test]
    public void InvalidFeedUrl_ShouldThrowValidationError()
    {
        var yaml = @"
feeds:
  - not-a-valid-url
rules:
  - prompt: 'Test'
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationLoader.LoadFromYaml(yaml));

        Assert.That(ex!.Message, Does.Contain("Invalid feed URL"));
    }

    [Test]
    public void NonHttpUrl_ShouldThrowValidationError()
    {
        var yaml = @"
feeds:
  - ftp://example.com/feed
rules:
  - prompt: 'Test'
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationLoader.LoadFromYaml(yaml));

        Assert.That(ex!.Message, Does.Contain("Invalid feed URL"));
        Assert.That(ex!.Message, Does.Contain("HTTP/HTTPS"));
    }

    [Test]
    public void LoadFromFile_NonexistentFile_ShouldThrow()
    {
        var ex = Assert.Throws<FileNotFoundException>(() =>
            ConfigurationLoader.LoadFromFile("/nonexistent/path/config.yaml"));

        Assert.That(ex!.Message, Does.Contain("not found"));
    }

    [Test]
    public void RealStackSifterYaml_ShouldLoadSuccessfully()
    {
        var configPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "stack-sifter.yaml"
        );

        if (!File.Exists(configPath))
        {
            Assert.Ignore($"stack-sifter.yaml not found at {configPath}");
            return;
        }

        var config = ConfigurationLoader.LoadFromFile(configPath);

        Assert.That(config.Feeds, Is.Not.Empty);
        Assert.That(config.Rules, Is.Not.Empty);
        Assert.That(config.PollIntervalMinutes, Is.EqualTo(5));

        foreach (var rule in config.Rules)
        {
            Assert.That(rule.Prompt, Is.Not.Empty);
        }
    }
}
