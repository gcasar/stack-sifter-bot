<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

This project is a C# solution for monitoring Stack Overflow RSS feeds, evaluating posts with LLM prompts, and notifying Slack channels. Use idiomatic, maintainable C# and follow TDD principles. Organize code for extensibility (interfaces for feed, LLM, and notification services).

- Start by creating NUnit tests that will specify the expected behavior of the system.
- Implement the code to pass the tests after.
- Use file-scoped namespaces.
- Be sure to split implementation into the src folder out of tests.

After every change run `dotnet test` to ensure all tests pass.