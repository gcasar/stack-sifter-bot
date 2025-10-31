# Configuration Guide

## Overview

Stack Sifter now supports YAML-based configuration for flexible post filtering and multi-channel notifications. This allows you to:

- Monitor multiple RSS feeds simultaneously
- Define multiple filtering rules with different criteria
- Send notifications to multiple Slack channels, emails, or webhooks
- Configure without modifying code

## Configuration File Format

### Basic Structure

```yaml
feeds:
  - https://stackoverflow.com/feeds/tag?tagnames=c%23&sort=newest
  - https://meta.stackoverflow.com/feeds

poll_interval_minutes: 5  # Optional - for future scheduled runs

rules:
  - prompt: "Your LLM prompt or filtering criteria"
    sifter_type: llm  # Options: llm (default), regex, tags, all
    notify:
      - slack: "#channel-name"
    tags: ["tag1", "tag2"]  # Optional pre-filter (not yet implemented)
```

### Complete Example

See `stack-sifter.yaml` in the repository root for a complete working example.

## Configuration Fields

### Root Level

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `feeds` | List[string] | Yes | List of RSS feed URLs to monitor |
| `poll_interval_minutes` | Integer | No | Polling interval (for future scheduled runs) |
| `rules` | List[Rule] | Yes | List of filtering rules |

### Rule Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `prompt` | String | Yes | LLM prompt or filtering criteria |
| `sifter_type` | String | No | Sifter type: "llm", "regex", "tags", "all" (default: "llm") |
| `notify` | List[NotificationTarget] | Yes | List of notification targets |
| `tags` | List[string] | No | Pre-filter tags (not yet implemented) |

### Notification Target Fields

Each notification target should specify **at least one** of:

| Field | Type | Description | Status |
|-------|------|-------------|--------|
| `slack` | String | Slack channel name (e.g., "#auth-team") or ID | Stub ready |
| `email` | String | Email address | Stub ready |
| `webhook` | String | Webhook URL for custom notifications | Stub ready |

## Usage

### Using Configuration File

```bash
# Run with configuration file
dotnet run --project src/StackSifter -- --config stack-sifter.yaml 2025-07-05T12:00:00Z

# Or use the short flag
dotnet run --project src/StackSifter -- -c stack-sifter.yaml 2025-07-05T12:00:00Z
```

### Legacy Mode (Backward Compatible)

```bash
# Legacy mode still works with hardcoded criteria
dotnet run --project src/StackSifter -- 2025-07-05T12:00:00Z
dotnet run --project src/StackSifter -- 2025-07-05T12:00:00Z https://meta.stackoverflow.com/feeds
```

## Multiple Notification Streams

You can send the same post to multiple channels:

```yaml
rules:
  - prompt: "Critical security issue?"
    notify:
      - slack: "#security-team"
      - slack: "#engineering-leads"
      - email: "security@example.com"
      - webhook: "https://example.com/hooks/security-alert"
```

When a post matches this rule, it will be sent to all four notification targets.

## Multiple Rules

You can define multiple rules that will all be evaluated for each post:

```yaml
rules:
  # Rule 1: Authentication issues
  - prompt: "Is this about authentication or OAuth?"
    notify:
      - slack: "#auth-team"

  # Rule 2: Performance issues
  - prompt: "Is this about performance or scalability?"
    notify:
      - slack: "#performance-team"

  # Rule 3: API breaking changes
  - prompt: "Does this mention breaking changes in our API?"
    notify:
      - slack: "#api-team"
      - slack: "#engineering-all"
```

A single post can match multiple rules and will trigger all matching notifications.

## Sifter Types

### LLM Sifter (default)

Uses OpenAI's GPT model to intelligently evaluate posts:

```yaml
rules:
  - prompt: "Is this related to async/await in C#?"
    sifter_type: llm  # or omit (default)
    notify:
      - slack: "#dotnet-experts"
```

**Requires:** `OPENAI_API_KEY` environment variable

### All Sifter

Matches all posts (useful for testing or catch-all rules):

```yaml
rules:
  - prompt: "All posts"
    sifter_type: all
    notify:
      - slack: "#all-posts"
```

### Future Sifter Types

- **regex**: Pattern-based matching (planned)
- **tags**: Tag-based filtering (planned)

## Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `OPENAI_API_KEY` | Yes | OpenAI API key for LLM sifting |
| `SLACK_WEBHOOK_URL` | No | Default Slack webhook URL (when not specified per-channel) |

## Validation

The configuration loader validates:

- At least one feed URL exists
- All feed URLs are valid HTTP/HTTPS URLs
- At least one rule exists
- Each rule has a non-empty prompt
- Each rule has at least one notification target
- Each notification target specifies at least one channel (slack/email/webhook)

Invalid configurations will throw clear error messages at startup.

## Output Format

The JSON output includes notification target information:

```json
{
  "TotalProcessed": 42,
  "LastCreated": "2025-07-05T15:30:00Z",
  "MatchingPosts": [
    {
      "Created": "2025-07-05T14:20:00Z",
      "Title": "How to implement OAuth in C#?",
      "Tags": ["c#", "oauth", "authentication"],
      "Url": "https://stackoverflow.com/questions/123456",
      "MatchReason": "Is this related to authentication?",
      "NotificationTargets": [
        "Slack: #auth-team",
        "Slack: #security-alerts"
      ]
    }
  ]
}
```

## Architecture

### Key Components

- **`StackSifterConfig`**: Root configuration model
- **`ConfigurationLoader`**: Loads and validates YAML
- **`SiftingRule`**: Individual rule definition
- **`NotificationTarget`**: Notification channel specification
- **`INotifier`**: Interface for notification implementations
  - `ConsoleNotifier`: Console output (default/fallback)
  - `SlackNotifier`: Slack integration (stub ready)
  - `CompositeNotifier`: Broadcasts to multiple notifiers

### Processing Flow

```
Load YAML Config
      ↓
Validate Configuration
      ↓
For each Feed:
  Fetch posts since timestamp
      ↓
  For each Rule:
    Create Sifter (LLM/Regex/Tags/All)
    Create Notifiers (Slack/Email/Webhook)
        ↓
    For each Post:
      Evaluate with Sifter
          ↓
      If match:
        Send to all Notifiers
        Record in results
      ↓
Output JSON Results
```

## Testing

Comprehensive tests are in `tests/StackSifter.Tests/WhenLoadingConfiguration.cs`:

- Valid configuration loading
- Multiple rules and notifications
- Tag and sifter_type handling
- Validation error cases
- URL validation
- Real config file validation

Run tests:
```bash
dotnet test
```

## Migration from Legacy Mode

If you're currently using the legacy CLI mode, migration is simple:

**Before (legacy):**
```bash
dotnet run -- 2025-07-05T12:00:00Z
```

**After (config file):**
1. Create `my-config.yaml`:
```yaml
feeds:
  - https://meta.stackoverflow.com/feeds
rules:
  - prompt: "Does this post contain a question about Python or C code?"
    notify:
      - slack: "#my-team"
```

2. Run:
```bash
dotnet run -- --config my-config.yaml 2025-07-05T12:00:00Z
```

## Future Enhancements

- [ ] Implement actual Slack webhook posting
- [ ] Email notification support
- [ ] Generic webhook support
- [ ] Regex-based sifter
- [ ] Tag-based pre-filtering
- [ ] Multiple LLM provider support (Azure OpenAI, local models)
- [ ] Rate limiting and retry logic
- [ ] Notification templates and formatting options
