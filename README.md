# stack-sifter
Follows Stack Overflow RSS feeds to sift according to some LLM prompt. One of the usecases is to notify teams on Slack about a possible question on meta that they might be interested in.

Imagine a person building a new feature adds a prompt describing it and the bot will gauge if a new bug report could be related to it. If it is, the bot will notify them via Slack.

Supports:

- [ ] Configurable prompts
- [ ] Notifying on matching posts via Slack
- [ ] Multiple conditions to notify on
- [ ] Multiple teams to notify

## Guiding principles

Part of my journey to become a vibe coder, a continuation of https://github.com/gcasar/stack-or-llm this project has some principles:
- **it should be quick** - 8 hours max for an MVP
- **dirty is not ok** - the end result will be gauged on its readability and maintainability
- **use Agentic Copilot** - Cursor was the name of the game before, lets compare them

With a few more specifics and/or nice to haves to try out:
- use C# - a strongly typed language where I'm drawn to a _proper IDE_ by default - how does the experience compare?
- **strive to use Test Driven Development** - early feedback and specification for the implementation - can we break the usual vibe coding mould
- use Pre-prompts - can we pre-prompt Copilot to fit our style
- use Codespaces, if possible - can we use Devcontainers to remove a lot of friction from developing

Using of @docs or any alternative is likely out of scope, because of the nature of the project. Every part of hte stack seems like something that the LLMs were already trained on, so it is unlikely to be a good fit.

## See also

Anyone interested in Stack Overflow bots should check out https://stackoverflow.blog/2019/09/17/meet-the-bots-that-help-moderate-stack-overflow/
