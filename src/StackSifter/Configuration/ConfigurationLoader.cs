using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace StackSifter.Configuration;

public class ConfigurationLoader
{
    public static StackSifterConfig LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
        }

        var yaml = File.ReadAllText(filePath);
        return LoadFromYaml(yaml);
    }

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
