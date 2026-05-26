using HoverTranslate.Core.Models;

namespace HoverTranslate.Core.Translators;

public interface ITranslator
{
    string ProviderName { get; }
    Task<TranslationResult> TranslateAsync(string text, CancellationToken cancellationToken = default);
}
