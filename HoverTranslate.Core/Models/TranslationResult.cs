namespace HoverTranslate.Core.Models;

public sealed class TranslationResult
{
    public bool Success { get; init; }
    public string SourceText { get; init; } = "";
    public string TranslatedText { get; init; } = "";
    public string? ErrorMessage { get; init; }
    public string Provider { get; init; } = "";
    public string? Model { get; init; }
    public int? PromptTokens { get; init; }
    public int? CompletionTokens { get; init; }
    public int? TotalTokens { get; init; }

    public static TranslationResult Ok(
        string source,
        string translated,
        string provider,
        string? model = null,
        int? promptTokens = null,
        int? completionTokens = null,
        int? totalTokens = null) =>
        new()
        {
            Success = true,
            SourceText = source,
            TranslatedText = translated,
            Provider = provider,
            Model = model,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens = totalTokens
        };

    public string FormatStatusSuffix()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(Model))
            parts.Add(Model.Trim());
        else if (!string.IsNullOrWhiteSpace(Provider))
            parts.Add(Provider.Trim());

        if (TotalTokens is > 0)
            parts.Add($"{TotalTokens} tokens");
        else if (PromptTokens is > 0 || CompletionTokens is > 0)
            parts.Add($"{(PromptTokens ?? 0) + (CompletionTokens ?? 0)} tokens");

        return parts.Count == 0 ? "" : string.Join(" · ", parts);
    }

    public static TranslationResult Fail(string source, string message) =>
        new() { Success = false, SourceText = source, ErrorMessage = message };
}
