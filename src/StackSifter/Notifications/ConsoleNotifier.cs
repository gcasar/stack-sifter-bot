using StackSifter.Feed;
using System.Text.Json;

namespace StackSifter.Notifications;

/// <summary>
/// Simple console-based notifier for testing and debugging.
/// Outputs notifications as formatted JSON to the console.
/// </summary>
public class ConsoleNotifier : INotifier
{
    private readonly string _targetDescription;

    public ConsoleNotifier(string targetDescription = "Console")
    {
        _targetDescription = targetDescription;
    }

    public Task NotifyAsync(Post post, string matchReason, CancellationToken cancellationToken = default)
    {
        var notification = new
        {
            Timestamp = DateTime.UtcNow,
            Target = _targetDescription,
            MatchReason = matchReason,
            Post = new
            {
                post.Title,
                post.Url,
                post.Published,
                post.Tags,
                post.Author,
                Brief = post.Brief.Length > 200 ? post.Brief.Substring(0, 200) + "..." : post.Brief
            }
        };

        var json = JsonSerializer.Serialize(notification, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        Console.WriteLine("=== NOTIFICATION ===");
        Console.WriteLine(json);
        Console.WriteLine("===================");

        return Task.CompletedTask;
    }
}
