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
    notify:
      - slack: '#auth-team'
";

        var config = ConfigurationLoader.LoadFromYaml(yaml);

        Assert.That(config.Feeds, Has.Count.EqualTo(2));
        Assert.That(config.Feeds[0], Is.EqualTo("https://stackoverflow.com/feeds/tag?tagnames=c%23&sort=newest"));
        Assert.That(config.PollIntervalMinutes, Is.EqualTo(5));
        Assert.That(config.Rules, Has.Count.EqualTo(1));
        Assert.That(config.Rules[0].Prompt, Is.EqualTo("Is this related to authentication?"));
        Assert.That(config.Rules[0].Notify, Has.Count.EqualTo(1));
        Assert.That(config.Rules[0].Notify[0].Slack, Is.EqualTo("#auth-team"));
    }

    [Test]
    public void MultipleRulesWithMultipleNotifications_ShouldLoadCorrectly()
    {
        var yaml = @"
feeds:
  - https://stackoverflow.com/feeds
rules:
  - prompt: 'First rule'
    notify:
      - slack: '#team1'
      - slack: '#team2'
      - email: 'team@example.com'
  - prompt: 'Second rule'
    notify:
      - webhook: 'https://example.com/hook'
";

        var config = ConfigurationLoader.LoadFromYaml(yaml);

        Assert.That(config.Rules, Has.Count.EqualTo(2));

        // First rule
        Assert.That(config.Rules[0].Notify, Has.Count.EqualTo(3));
        Assert.That(config.Rules[0].Notify[0].Slack, Is.EqualTo("#team1"));
        Assert.That(config.Rules[0].Notify[1].Slack, Is.EqualTo("#team2"));
        Assert.That(config.Rules[0].Notify[2].Email, Is.EqualTo("team@example.com"));

        // Second rule
        Assert.That(config.Rules[1].Notify, Has.Count.EqualTo(1));
        Assert.That(config.Rules[1].Notify[0].Webhook, Is.EqualTo("https://example.com/hook"));
    }

    [Test]
    public void ConfigurationWithTags_ShouldLoadTags()
    {
        var yaml = @"
feeds:
  - https://stackoverflow.com/feeds
rules:
  - prompt: 'Test'
    tags: ['authentication', 'login', 'oauth']
    notify:
      - slack: '#test'
";

        var config = ConfigurationLoader.LoadFromYaml(yaml);

        Assert.That(config.Rules[0].Tags, Is.Not.Null);
        Assert.That(config.Rules[0].Tags, Has.Count.EqualTo(3));
        Assert.That(config.Rules[0].Tags, Contains.Item("authentication"));
        Assert.That(config.Rules[0].Tags, Contains.Item("login"));
        Assert.That(config.Rules[0].Tags, Contains.Item("oauth"));
    }

    [Test]
    public void ConfigurationWithSifterType_ShouldLoadSifterType()
    {
        var yaml = @"
feeds:
  - https://stackoverflow.com/feeds
rules:
  - prompt: 'Test'
    sifter_type: regex
    notify:
      - slack: '#test'
";

        var config = ConfigurationLoader.LoadFromYaml(yaml);

        Assert.That(config.Rules[0].SifterType, Is.EqualTo("regex"));
    }

    [Test]
    public void NoFeeds_ShouldThrowValidationError()
    {
        var yaml = @"
feeds: []
rules:
  - prompt: 'Test'
    notify:
      - slack: '#test'
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
    notify:
      - slack: '#test'
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationLoader.LoadFromYaml(yaml));

        Assert.That(ex!.Message, Does.Contain("non-empty prompt"));
    }

    [Test]
    public void NoNotifications_ShouldThrowValidationError()
    {
        var yaml = @"
feeds:
  - https://stackoverflow.com/feeds
rules:
  - prompt: 'Test'
    notify: []
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationLoader.LoadFromYaml(yaml));

        Assert.That(ex!.Message, Does.Contain("at least one notification target"));
    }

    [Test]
    public void NotificationWithNoChannel_ShouldThrowValidationError()
    {
        var yaml = @"
feeds:
  - https://stackoverflow.com/feeds
rules:
  - prompt: 'Test'
    notify:
      - slack: ''
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationLoader.LoadFromYaml(yaml));

        Assert.That(ex!.Message, Does.Contain("at least one channel"));
    }

    [Test]
    public void InvalidFeedUrl_ShouldThrowValidationError()
    {
        var yaml = @"
feeds:
  - not-a-valid-url
rules:
  - prompt: 'Test'
    notify:
      - slack: '#test'
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
    notify:
      - slack: '#test'
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationLoader.LoadFromYaml(yaml));

        Assert.That(ex!.Message, Does.Contain("Invalid feed URL"));
        Assert.That(ex!.Message, Does.Contain("HTTP/HTTPS"));
    }

    [Test]
    public void NotificationTarget_GetDescription_ReturnsCorrectFormat()
    {
        var slackTarget = new NotificationTarget { Slack = "#test-channel" };
        var emailTarget = new NotificationTarget { Email = "test@example.com" };
        var webhookTarget = new NotificationTarget { Webhook = "https://example.com/hook" };

        Assert.That(slackTarget.GetDescription(), Is.EqualTo("Slack: #test-channel"));
        Assert.That(emailTarget.GetDescription(), Is.EqualTo("Email: test@example.com"));
        Assert.That(webhookTarget.GetDescription(), Is.EqualTo("Webhook: https://example.com/hook"));
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
        // This test loads the actual stack-sifter.yaml from the repository
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

        // Basic validation that it loaded
        Assert.That(config.Feeds, Is.Not.Empty);
        Assert.That(config.Rules, Is.Not.Empty);
        Assert.That(config.PollIntervalMinutes, Is.EqualTo(5));

        // Verify all rules have valid structure
        foreach (var rule in config.Rules)
        {
            Assert.That(rule.Prompt, Is.Not.Empty);
            Assert.That(rule.Notify, Is.Not.Empty);

            foreach (var target in rule.Notify)
            {
                Assert.That(target.GetDescription(), Is.Not.EqualTo("Unknown notification target"));
            }
        }
    }
}
