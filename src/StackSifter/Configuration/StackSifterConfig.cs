namespace StackSifter.Configuration;

/// <summary>
/// Root configuration for the Stack Sifter application.
/// </summary>
public class StackSifterConfig
{
    /// <summary>
    /// List of RSS feed URLs to monitor.
    /// </summary>
    public List<string> Feeds { get; set; } = new();
    
    /// <summary>
    /// Optional polling interval in minutes (not currently used by the CLI).
    /// </summary>
    public int? PollIntervalMinutes { get; set; }
    
    /// <summary>
    /// List of sifting rules to evaluate posts against.
    /// </summary>
    public List<SiftingRule> Rules { get; set; } = new();
}

/// <summary>
/// Represents a rule for evaluating and routing posts.
/// </summary>
public class SiftingRule
{
    /// <summary>
    /// The LLM prompt used to evaluate whether a post matches this rule.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;
    
    /// <summary>
    /// List of notification targets to alert when a post matches.
    /// </summary>
    public List<NotificationTarget> Notify { get; set; } = new();
}

/// <summary>
/// Represents a notification destination.
/// </summary>
public class NotificationTarget
{
    /// <summary>
    /// The Slack channel to notify (e.g., "#team-channel").
    /// </summary>
    public string Slack { get; set; } = string.Empty;
}
