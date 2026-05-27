using System.Text.RegularExpressions;

namespace HoverTranslate.Core.Services;

public static partial class ConfigRedaction
{
    public static string RedactApiKeys(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return "{}";

        return ApiKeyPattern().Replace(json, "$1\"***\"");
    }

    public static string ReadRedactedConfigFile()
    {
        if (!File.Exists(ConfigService.ConfigFilePath))
            return "{}";

        return RedactApiKeys(File.ReadAllText(ConfigService.ConfigFilePath));
    }

    [GeneratedRegex("(?i)(\"apiKey\"\\s*:\\s*)\"[^\"]*\"", RegexOptions.Compiled)]
    private static partial Regex ApiKeyPattern();
}
