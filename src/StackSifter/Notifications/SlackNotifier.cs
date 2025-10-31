using StackSifter.Feed;

namespace StackSifter.Notifications;

/// <summary>
/// Slack-based notifier (ready for implementation).
/// TODO: Implement actual Slack webhook or bot API integration.
/// </summary>
public class SlackNotifier : INotifier
{
    private readonly string _channel;
    private readonly string? _webhookUrl;

    /// <summary>
    /// Creates a new Slack notifier.
    /// </summary>
    /// <param name="channel">Slack channel name (e.g., "#auth-team") or ID.</param>
    /// <param name="webhookUrl">Optional webhook URL. If not provided, will use env var SLACK_WEBHOOK_URL.</param>
    public SlackNotifier(string channel, string? webhookUrl = null)
    {
        _channel = channel;
        _webhookUrl = webhookUrl ?? Environment.GetEnvironmentVariable("SLACK_WEBHOOK_URL");
    }

    public Task NotifyAsync(Post post, string matchReason, CancellationToken cancellationToken = default)
    {
        // TODO: Implement Slack notification
        // For now, just log to console that we would send to Slack
        Console.WriteLine($"[SLACK STUB] Would send notification to {_channel}:");
        Console.WriteLine($"  Title: {post.Title}");
        Console.WriteLine($"  URL: {post.Url}");
        Console.WriteLine($"  Reason: {matchReason}");
        Console.WriteLine($"  Tags: {string.Join(", ", post.Tags)}");

        // Future implementation steps:
        // 1. Format message as Slack Block Kit JSON or simple text
        // 2. POST to webhook URL or use Slack Web API
        // 3. Handle rate limiting and retries
        // 4. Return success/failure status

        return Task.CompletedTask;
    }

    /// <summary>
    /// Future: Format post as Slack message blocks.
    /// </summary>
    private object FormatSlackMessage(Post post, string matchReason)
    {
        // Example Slack Block Kit message structure
        return new
        {
            channel = _channel,
            blocks = new[]
            {
                new
                {
                    type = "header",
                    text = new
                    {
                        type = "plain_text",
                        text = post.Title
                    }
                },
                new
                {
                    type = "section",
                    fields = new[]
                    {
                        new { type = "mrkdwn", text = $"*Author:*\n{post.Author}" },
                        new { type = "mrkdwn", text = $"*Tags:*\n{string.Join(", ", post.Tags)}" }
                    }
                },
                new
                {
                    type = "section",
                    text = new
                    {
                        type = "mrkdwn",
                        text = $"*Match Reason:* {matchReason}\n\n{post.Brief}"
                    }
                },
                new
                {
                    type = "actions",
                    elements = new[]
                    {
                        new
                        {
                            type = "button",
                            text = new { type = "plain_text", text = "View on Stack Overflow" },
                            url = post.Url
                        }
                    }
                }
            }
        };
    }
}
