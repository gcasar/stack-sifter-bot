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
    public List<NotificationTarget>? Notify { get; set; }
}

public class NotificationTarget
{
    public string? Slack { get; set; }
    public string? Email { get; set; }
    public string? Webhook { get; set; }
}
