namespace StackSifter.Configuration;

public class StackSifterConfig
{
    public List<string> Feeds { get; set; } = new();
    public int? PollIntervalMinutes { get; set; }
    public List<SiftingRule> Rules { get; set; } = new();
}

public class SiftingRule
{
    public string Prompt { get; set; } = string.Empty;
    public string Slack { get; set; } = string.Empty;
}
