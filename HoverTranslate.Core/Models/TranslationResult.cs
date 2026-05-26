namespace HoverTranslate.Core.Models;

public sealed class TranslationResult
{
    public bool Success { get; init; }
    public string SourceText { get; init; } = "";
    public string TranslatedText { get; init; } = "";
    public string? ErrorMessage { get; init; }
    public string Provider { get; init; } = "";

    public static TranslationResult Ok(string source, string translated, string provider) =>
        new() { Success = true, SourceText = source, TranslatedText = translated, Provider = provider };

    public static TranslationResult Fail(string source, string message) =>
        new() { Success = false, SourceText = source, ErrorMessage = message };
}
