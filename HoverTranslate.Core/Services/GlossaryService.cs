namespace HoverTranslate.Core.Services;

public sealed class GlossaryService
{
    public string ApplyPostTranslation(string translated, IReadOnlyDictionary<string, string> glossary)
    {
        var result = translated;
        foreach (var (en, zh) in glossary.OrderByDescending(kv => kv.Key.Length))
        {
            if (string.IsNullOrEmpty(en)) continue;
            result = result.Replace(en, zh, StringComparison.OrdinalIgnoreCase);
        }
        return result;
    }

    public string BuildGlossaryPromptSection(IReadOnlyDictionary<string, string> glossary)
    {
        if (glossary.Count == 0) return "（无术语表）";
        return string.Join(", ", glossary.Select(kv => $"{kv.Key}={kv.Value}"));
    }
}
