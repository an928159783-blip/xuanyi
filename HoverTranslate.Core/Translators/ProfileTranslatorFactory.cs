using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.Core.Translators;

public static class ProfileTranslatorFactory
{
    public static ITranslator Create(ApiProfile profile, AppConfig config, GlossaryService glossary)
    {
        var kind = profile.Kind?.Trim().ToLowerInvariant() ?? ApiProfileKinds.OpenAiCompatible;
        return kind switch
        {
            ApiProfileKinds.Microsoft => new MicrosoftTranslator(profile, config),
            ApiProfileKinds.Google => new GoogleTranslator(profile, config),
            _ => new GenericLlmTranslator(profile, config, glossary)
        };
    }
}
