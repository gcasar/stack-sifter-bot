using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace StackSifter.Configuration;

/// <summary>
/// Loads and validates Stack Sifter configuration from YAML files.
/// </summary>
public class ConfigurationLoader
{
    /// <summary>
    /// Loads configuration from a YAML file.
    /// </summary>
    /// <param name="filePath">Path to the YAML configuration file.</param>
    /// <returns>A validated StackSifterConfig instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the configuration is invalid.</exception>
    public static StackSifterConfig LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
        }

        var yaml = File.ReadAllText(filePath);
        return LoadFromYaml(yaml);
    }

    /// <summary>
    /// Loads configuration from a YAML string.
    /// </summary>
    /// <param name="yaml">YAML content to parse.</param>
    /// <returns>A validated StackSifterConfig instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the configuration is invalid.</exception>
    public static StackSifterConfig LoadFromYaml(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var config = deserializer.Deserialize<StackSifterConfig>(yaml);

        ValidateConfig(config);

        return config;
    }

    private static void ValidateConfig(StackSifterConfig config)
    {
        if (config.Feeds?.Any() != true)
            throw new InvalidOperationException("Configuration must contain at least one feed URL.");

        // Validate feed URLs
        foreach (var feed in config.Feeds)
        {
            if (!Uri.TryCreate(feed, UriKind.Absolute, out var uri) || 
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException($"Invalid feed URL: {feed}. URLs must be valid HTTP or HTTPS addresses.");
            }
        }

        if (config.Rules?.Any() != true)
            throw new InvalidOperationException("Configuration must contain at least one sifting rule.");

        var invalidRule = config.Rules
            .Select((rule, index) => new { rule, index })
            .FirstOrDefault(x => string.IsNullOrWhiteSpace(x.rule.Prompt) ||
                                x.rule.Notify?.Any() != true ||
                                x.rule.Notify.Any(n => string.IsNullOrWhiteSpace(n.Slack)));

        if (invalidRule != null)
        {
            var rule = invalidRule.rule;
            if (string.IsNullOrWhiteSpace(rule.Prompt))
                throw new InvalidOperationException($"Rule {invalidRule.index} must have a non-empty prompt.");
            if (rule.Notify?.Any() != true)
                throw new InvalidOperationException($"Rule {invalidRule.index} must have at least one notification target.");
            throw new InvalidOperationException($"Rule {invalidRule.index} must have a non-empty slack channel.");
        }
    }
}
