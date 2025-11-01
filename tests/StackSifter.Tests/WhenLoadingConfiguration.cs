using StackSifter.Configuration;

namespace StackSifter.Tests;

[TestFixture]
public class WhenLoadingConfiguration
{
    [Test]
    public void ValidConfiguration_ShouldLoadSuccessfully()
    {
        var config = ConfigurationLoader.LoadFromYaml(@"
feeds:
  - https://stackoverflow.com/feeds/tag?tagnames=c%23&sort=newest
  - https://meta.stackoverflow.com/feeds
poll_interval_minutes: 5
rules:
  - prompt: 'Is this related to authentication?'
    notify:
      - slack: '#auth-team'
");

        Assert.That(config.Feeds, Has.Count.EqualTo(2));
        Assert.That(config.PollIntervalMinutes, Is.EqualTo(5));
        Assert.That(config.Rules[0].Prompt, Is.EqualTo("Is this related to authentication?"));
        Assert.That(config.Rules[0].Notify[0].Slack, Is.EqualTo("#auth-team"));
    }

    [Test]
    public void MultipleRules_ShouldLoadCorrectly()
    {
        var config = ConfigurationLoader.LoadFromYaml(@"
feeds: [https://stackoverflow.com/feeds]
rules:
  - prompt: 'First'
    notify: [{slack: '#team1'}]
  - prompt: 'Second'
    notify: [{slack: '#team2'}]
");

        Assert.That(config.Rules, Has.Count.EqualTo(2));
        Assert.That(config.Rules[0].Notify[0].Slack, Is.EqualTo("#team1"));
        Assert.That(config.Rules[1].Notify[0].Slack, Is.EqualTo("#team2"));
    }

    [Test]
    [TestCase("feeds: []\nrules: [{prompt: 'Test', notify: [{slack: '#test'}]}]", "at least one feed")]
    [TestCase("feeds: [https://stackoverflow.com/feeds]\nrules: []", "at least one sifting rule")]
    [TestCase("feeds: [https://stackoverflow.com/feeds]\nrules: [{prompt: '', notify: [{slack: '#test'}]}]", "non-empty prompt")]
    [TestCase("feeds: [https://stackoverflow.com/feeds]\nrules: [{prompt: 'Test', notify: []}]", "at least one notification")]
    [TestCase("feeds: [https://stackoverflow.com/feeds]\nrules: [{prompt: 'Test', notify: [{slack: ''}]}]", "non-empty slack")]
    [TestCase("feeds: [https://stackoverflow.com/feeds]\nrules: [{prompt: 'Test'}]", "at least one notification")]
    public void InvalidConfiguration_ShouldThrowValidationError(string yaml, string expectedError)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigurationLoader.LoadFromYaml(yaml));
        Assert.That(ex!.Message, Does.Contain(expectedError));
    }

    [Test]
    public void LoadFromFile_NonexistentFile_ShouldThrow()
    {
        var ex = Assert.Throws<FileNotFoundException>(() =>
            ConfigurationLoader.LoadFromFile("/nonexistent/path/config.yaml"));
        Assert.That(ex!.Message, Does.Contain("not found"));
    }
}
