namespace StackSifter.Configuration;

/// <summary>
/// Root configuration model for the stack-sifter application.
/// </summary>
public class StackSifterConfig
{
    /// <summary>
    /// List of Stack Overflow RSS feed URLs to monitor.
    /// </summary>
    public List<string> Feeds { get; set; } = new();

    /// <summary>
    /// How often to poll feeds in minutes (optional, for future scheduled use).
    /// </summary>
    public int? PollIntervalMinutes { get; set; }

    /// <summary>
    /// List of rules to apply when sifting posts.
    /// </summary>
    public List<SiftingRule> Rules { get; set; } = new();
}

/// <summary>
/// Represents a single rule for filtering/sifting posts.
/// </summary>
public class SiftingRule
{
    /// <summary>
    /// The prompt/criteria to evaluate posts against (sent to LLM or used for regex matching).
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// List of notification targets when a post matches this rule.
    /// </summary>
    public List<NotificationTarget> Notify { get; set; } = new();

    /// <summary>
    /// Optional tags to pre-filter posts before applying the prompt (not yet implemented).
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Optional sifter type: "llm" (default), "regex", "tags", etc.
    /// </summary>
    public string? SifterType { get; set; }
}

/// <summary>
/// Represents a notification target (Slack, email, webhook, etc.).
/// </summary>
public class NotificationTarget
{
    /// <summary>
    /// Slack channel name or ID (e.g., "#auth-team" or "C1234567890").
    /// </summary>
    public string? Slack { get; set; }

    /// <summary>
    /// Email address to notify (future enhancement).
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Webhook URL to POST notification to (future enhancement).
    /// </summary>
    public string? Webhook { get; set; }

    /// <summary>
    /// Returns a human-readable description of this notification target.
    /// </summary>
    public string GetDescription()
    {
        if (!string.IsNullOrEmpty(Slack))
            return $"Slack: {Slack}";
        if (!string.IsNullOrEmpty(Email))
            return $"Email: {Email}";
        if (!string.IsNullOrEmpty(Webhook))
            return $"Webhook: {Webhook}";
        return "Unknown notification target";
    }
}
