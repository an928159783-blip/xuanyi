using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.Core.Translators;

public sealed class GenericLlmTranslator : LlmTranslatorBase
{
    public GenericLlmTranslator(LlmProviderConfig profile, string providerId, AppConfig config, GlossaryService glossary)
        : base(profile, providerId, config, glossary)
    {
    }
}
