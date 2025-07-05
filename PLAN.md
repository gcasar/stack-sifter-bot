# Implementation details for stack-sifter

## Configuration file (YAML)
- The system uses a YAML file (e.g., `stack-sifter.yaml`) to define:
  - **Feeds**: List of Stack Overflow RSS feed URLs to monitor (can be tag-specific or site-wide).
  - **Polling interval**: How often to check feeds (e.g., every 5 minutes).
  - **Prompts/Rules**: Each rule includes:
    - A prompt or condition (plain text, regex, or LLM prompt)
    - List of Slack channels or teams to notify
    - Optional filters (e.g., tags, post type)

Example:
```yaml
feeds:
  - https://stackoverflow.com/feeds/tag?tagnames=c%23&sort=newest
  - https://meta.stackoverflow.com/feeds
poll_interval_minutes: 5
rules:
  - prompt: "Is this related to our new authentication system?"
    notify:
      - slack: "#auth-team"
    tags: ["authentication", "login"]
  - prompt: "Does this post mention our API v2?"
    notify:
      - slack: "#api-team"
```

## RSS Feed Interface
- The service periodically (per config) fetches and parses each RSS feed.
- New posts are detected by tracking post IDs or timestamps.
- Each new post is passed through the configured rules.

## LLM Integration
- For each rule with an LLM prompt, the post content (title, body, tags) is sent to the LLM (e.g., OpenAI, Azure OpenAI, or local model).
- The LLM is asked to answer if the post matches the rule's intent (e.g., via a yes/no or score response).
- Posts that match are collected for notification.
- LLM API keys and endpoints are provided via environment variables or a secrets file.

## Slack Notification
- For each matching post, a message is sent to the configured Slack channel(s) using a Slack webhook or bot token.
- Message includes post title, link, and a summary of why it matched.
