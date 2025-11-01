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
    slack: '#auth-team'
";

        var config = ConfigurationLoader.LoadFromYaml(yaml);

        Assert.That(config.Feeds, Has.Count.EqualTo(2));
        Assert.That(config.Feeds[0], Is.EqualTo("https://stackoverflow.com/feeds/tag?tagnames=c%23&sort=newest"));
        Assert.That(config.PollIntervalMinutes, Is.EqualTo(5));
        Assert.That(config.Rules, Has.Count.EqualTo(1));
        Assert.That(config.Rules[0].Prompt, Is.EqualTo("Is this related to authentication?"));
        Assert.That(config.Rules[0].Slack, Is.EqualTo("#auth-team"));
    }

    [Test]
    public void MultipleRules_ShouldLoadCorrectly()
    {
        var yaml = @"
feeds:
  - https://stackoverflow.com/feeds
rules:
  - prompt: 'First rule'
    slack: '#team1'
  - prompt: 'Second rule'
    slack: '#team2'
  - prompt: 'Third rule'
    slack: '#team3'
";

        var config = ConfigurationLoader.LoadFromYaml(yaml);

        Assert.That(config.Rules, Has.Count.EqualTo(3));
        Assert.That(config.Rules[0].Prompt, Is.EqualTo("First rule"));
        Assert.That(config.Rules[0].Slack, Is.EqualTo("#team1"));
        Assert.That(config.Rules[1].Prompt, Is.EqualTo("Second rule"));
        Assert.That(config.Rules[1].Slack, Is.EqualTo("#team2"));
        Assert.That(config.Rules[2].Prompt, Is.EqualTo("Third rule"));
        Assert.That(config.Rules[2].Slack, Is.EqualTo("#team3"));
    }

    [Test]
    public void NoFeeds_ShouldThrowValidationError()
    {
        var yaml = @"
feeds: []
rules:
  - prompt: 'Test'
    slack: '#test'
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
    slack: '#test'
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationLoader.LoadFromYaml(yaml));

        Assert.That(ex!.Message, Does.Contain("non-empty prompt"));
    }

    [Test]
    public void EmptySlack_ShouldThrowValidationError()
    {
        var yaml = @"
feeds:
  - https://stackoverflow.com/feeds
rules:
  - prompt: 'Test'
    slack: ''
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationLoader.LoadFromYaml(yaml));

        Assert.That(ex!.Message, Does.Contain("non-empty slack channel"));
    }

    [Test]
    public void MissingSlack_ShouldThrowValidationError()
    {
        var yaml = @"
feeds:
  - https://stackoverflow.com/feeds
rules:
  - prompt: 'Test'
";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ConfigurationLoader.LoadFromYaml(yaml));

        Assert.That(ex!.Message, Does.Contain("non-empty slack channel"));
    }

    [Test]
    public void LoadFromFile_NonexistentFile_ShouldThrow()
    {
        var ex = Assert.Throws<FileNotFoundException>(() =>
            ConfigurationLoader.LoadFromFile("/nonexistent/path/config.yaml"));

        Assert.That(ex!.Message, Does.Contain("not found"));
    }
}
