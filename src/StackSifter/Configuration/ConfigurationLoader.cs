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
        if (config.Feeds?.Any() != true)
        {
            throw new InvalidOperationException("Configuration must contain at least one feed URL.");
        }

        if (config.Rules?.Any() != true)
        {
            throw new InvalidOperationException("Configuration must contain at least one sifting rule.");
        }

        var emptyPromptRule = config.Rules
            .Select((rule, index) => new { Rule = rule, Index = index })
            .FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Rule.Prompt));

        if (emptyPromptRule != null)
        {
            throw new InvalidOperationException($"Rule {emptyPromptRule.Index} must have a non-empty prompt.");
        }

        var invalidFeed = config.Feeds
            .FirstOrDefault(feed => !Uri.TryCreate(feed, UriKind.Absolute, out var uri) ||
                                   (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps));

        if (invalidFeed != null)
        {
            throw new InvalidOperationException($"Invalid feed URL: {invalidFeed}. Must be a valid HTTP/HTTPS URL.");
        }
    }
}
