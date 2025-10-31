using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace StackSifter.Configuration;

/// <summary>
/// Loads and validates StackSifter configuration from YAML files.
/// </summary>
public class ConfigurationLoader
{
    /// <summary>
    /// Loads configuration from a YAML file.
    /// </summary>
    /// <param name="filePath">Path to the YAML configuration file.</param>
    /// <returns>Parsed and validated configuration.</returns>
    /// <exception cref="FileNotFoundException">If the config file doesn't exist.</exception>
    /// <exception cref="InvalidOperationException">If the config is invalid.</exception>
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
    /// <param name="yaml">YAML content as a string.</param>
    /// <returns>Parsed and validated configuration.</returns>
    /// <exception cref="InvalidOperationException">If the config is invalid.</exception>
    public static StackSifterConfig LoadFromYaml(string yaml)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var config = deserializer.Deserialize<StackSifterConfig>(yaml);

        ValidateConfig(config);

        return config;
    }

    /// <summary>
    /// Validates the configuration for required fields and logical consistency.
    /// </summary>
    private static void ValidateConfig(StackSifterConfig config)
    {
        if (config.Feeds == null || config.Feeds.Count == 0)
        {
            throw new InvalidOperationException("Configuration must contain at least one feed URL.");
        }

        if (config.Rules == null || config.Rules.Count == 0)
        {
            throw new InvalidOperationException("Configuration must contain at least one sifting rule.");
        }

        for (int i = 0; i < config.Rules.Count; i++)
        {
            var rule = config.Rules[i];

            if (string.IsNullOrWhiteSpace(rule.Prompt))
            {
                throw new InvalidOperationException($"Rule {i} must have a non-empty prompt.");
            }

            if (rule.Notify == null || rule.Notify.Count == 0)
            {
                throw new InvalidOperationException($"Rule {i} must have at least one notification target.");
            }

            for (int j = 0; j < rule.Notify.Count; j++)
            {
                var target = rule.Notify[j];
                if (string.IsNullOrWhiteSpace(target.Slack) &&
                    string.IsNullOrWhiteSpace(target.Email) &&
                    string.IsNullOrWhiteSpace(target.Webhook))
                {
                    throw new InvalidOperationException(
                        $"Rule {i}, notification target {j} must specify at least one channel (slack, email, or webhook).");
                }
            }
        }

        // Validate feed URLs are properly formatted
        foreach (var feed in config.Feeds)
        {
            if (!Uri.TryCreate(feed, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException($"Invalid feed URL: {feed}. Must be a valid HTTP/HTTPS URL.");
            }
        }
    }
}
