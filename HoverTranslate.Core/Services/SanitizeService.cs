using System.Text.RegularExpressions;

namespace HoverTranslate.Core.Services;

public sealed class SanitizeService
{
    private static readonly Regex[] Patterns =
    [
        new(@"sk-[a-zA-Z0-9]{20,}", RegexOptions.Compiled),
        new(@"Bearer\s+\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"password\s*=\s*\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"api[_-]?key\s*[:=]\s*\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled),
    ];

    public (string Text, bool HadSensitive) Sanitize(string input)
    {
        var hadSensitive = false;
        var text = input;
        foreach (var pattern in Patterns)
        {
            if (pattern.IsMatch(text))
                hadSensitive = true;
            text = pattern.Replace(text, "[REDACTED]");
        }
        return (text, hadSensitive);
    }

    public bool LooksLikeCode(string text)
    {
        if (text.Length < 20) return false;
        var markers = text.Count(c => c is '{' or '}' or ';' or '(' or ')');
        return markers > text.Length / 15;
    }
}
