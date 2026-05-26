using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.Core.Translators;

public static class TranslatorFactory
{
    public static ITranslator Create(AppConfig config, GlossaryService glossary) =>
        ProviderResolver.CreateTranslator(config, glossary);
}
