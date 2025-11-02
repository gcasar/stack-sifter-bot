using System.Diagnostics;
using System.Text.Json;

namespace StackSifter.Tests;

/// <summary>
/// Integration smoke test that validates end-to-end functionality by invoking the CLI application
/// exactly as it would be run in production. This catches compilation errors and validates the
/// full execution path including argument parsing, configuration loading, and JSON output.
/// Requires OPENAI_API_KEY environment variable and makes real API calls.
/// </summary>
[TestFixture]
[Explicit("Requires OPENAI_API_KEY environment variable and makes real API calls")]
public class IntegrationSmokeTest
{
    private string? _testConfigPath;
    private string? _projectPath;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Find the project root (where the .csproj file is)
        var testAssemblyPath = TestContext.CurrentContext.TestDirectory;
        var current = new DirectoryInfo(testAssemblyPath);

        // Navigate up to find the solution root
        while (current != null && !File.Exists(Path.Combine(current.FullName, "stack-sifter.yaml")))
        {
            current = current.Parent;
        }

        if (current == null)
        {
            throw new InvalidOperationException("Could not find project root directory");
        }

        _projectPath = Path.Combine(current.FullName, "src", "StackSifter", "StackSifter.csproj");

        // Create test configuration file
        _testConfigPath = Path.Combine(Path.GetTempPath(), $"stack-sifter-test-{Guid.NewGuid()}.yaml");
        File.WriteAllText(_testConfigPath, @"
feeds:
  - https://stackoverflow.com/feeds/tag?tagnames=c%23&sort=newest
rules:
  - prompt: 'Does this mention async/await or Task-related issues?'
    notify:
      - slack: '#test-channel'
");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        // Clean up test config file
        if (_testConfigPath != null && File.Exists(_testConfigPath))
        {
            File.Delete(_testConfigPath);
        }
    }

    [Test]
    public async Task CliSmokeTest_InvokeApplicationWithRealArguments()
    {
        // Arrange - Check for API key
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Ignore("OPENAI_API_KEY environment variable not set. Skipping integration test.");
            return;
        }

        Assert.That(_projectPath, Is.Not.Null, "Project path should be set");
        Assert.That(_testConfigPath, Is.Not.Null, "Test config path should be set");
        Assert.That(File.Exists(_testConfigPath), Is.True, "Test config file should exist");

        // Use a recent timestamp to minimize data (last 6 hours)
        var since = DateTime.UtcNow.AddHours(-6).ToString("yyyy-MM-ddTHH:mm:ssZ");

        TestContext.WriteLine($"Running CLI with config: {_testConfigPath}");
        TestContext.WriteLine($"Since timestamp: {since}");

        // Act - Invoke the application exactly as it would run in production
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{_projectPath}\" --configuration Release -- \"{_testConfigPath}\" \"{since}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.Environment["OPENAI_API_KEY"] = apiKey;

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null) outputBuilder.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null) errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait for completion with timeout
        var completed = await Task.Run(() => process.WaitForExit(120000)); // 2 minute timeout

        if (!completed)
        {
            process.Kill();
            Assert.Fail("Application did not complete within timeout period");
        }

        var output = outputBuilder.ToString();
        var errorOutput = errorBuilder.ToString();

        TestContext.WriteLine("=== Standard Error ===");
        TestContext.WriteLine(errorOutput);
        TestContext.WriteLine("=== Standard Output ===");
        TestContext.WriteLine(output);

        // Assert - Verify successful execution
        Assert.That(process.ExitCode, Is.EqualTo(0),
            $"Application should exit with code 0. Exit code: {process.ExitCode}\nStderr: {errorOutput}");

        Assert.That(output, Is.Not.Empty, "Application should produce output");

        // Validate JSON output
        JsonDocument? jsonDoc = null;
        Assert.DoesNotThrow(() =>
        {
            jsonDoc = JsonDocument.Parse(output);
        }, "Output should be valid JSON");

        Assert.That(jsonDoc, Is.Not.Null, "JSON document should be parsed");

        var root = jsonDoc!.RootElement;
        Assert.That(root.TryGetProperty("TotalProcessed", out var totalProcessed), Is.True,
            "JSON should contain TotalProcessed field");
        Assert.That(root.TryGetProperty("LastCreated", out _), Is.True,
            "JSON should contain LastCreated field");
        Assert.That(root.TryGetProperty("MatchingPosts", out var matchingPosts), Is.True,
            "JSON should contain MatchingPosts field");

        // Log results
        TestContext.WriteLine("=== Test Results ===");
        TestContext.WriteLine($"Total Posts Processed: {totalProcessed.GetInt32()}");
        TestContext.WriteLine($"Matching Posts: {matchingPosts.GetArrayLength()}");

        if (matchingPosts.GetArrayLength() > 0)
        {
            TestContext.WriteLine("\nMatched Posts:");
            foreach (var post in matchingPosts.EnumerateArray().Take(3))
            {
                if (post.TryGetProperty("Title", out var title))
                {
                    TestContext.WriteLine($"  - {title.GetString()}");
                }
            }
        }

        // Basic validation
        Assert.That(totalProcessed.GetInt32(), Is.GreaterThanOrEqualTo(0),
            "Should process zero or more posts");
        Assert.That(matchingPosts.ValueKind, Is.EqualTo(JsonValueKind.Array),
            "MatchingPosts should be an array");
    }

    [Test]
    public async Task CliSmokeTest_ValidatesInvalidArguments()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Ignore("OPENAI_API_KEY environment variable not set. Skipping integration test.");
            return;
        }

        Assert.That(_projectPath, Is.Not.Null, "Project path should be set");

        // Act - Invoke with invalid timestamp
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{_projectPath}\" --configuration Release -- \"{_testConfigPath}\" \"invalid-timestamp\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.Environment["OPENAI_API_KEY"] = apiKey;

        using var process = Process.Start(startInfo);
        Assert.That(process, Is.Not.Null);

        await Task.Run(() => process!.WaitForExit(30000));
        var errorOutput = await process!.StandardError.ReadToEndAsync();

        TestContext.WriteLine("=== Error Output ===");
        TestContext.WriteLine(errorOutput);

        // Assert - Should fail with non-zero exit code
        Assert.That(process.ExitCode, Is.Not.EqualTo(0),
            "Application should exit with non-zero code for invalid arguments");
        Assert.That(errorOutput, Does.Contain("Invalid timestamp").Or.Contain("Usage:"),
            "Error message should indicate invalid timestamp or show usage");
    }
}
