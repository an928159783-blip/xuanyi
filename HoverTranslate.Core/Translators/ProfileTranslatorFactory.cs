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
            ApiProfileKinds.Microsoft => new MicrosoftTranslator(profile),
            ApiProfileKinds.Google => new GoogleTranslator(profile),
            _ => new GenericLlmTranslator(ProviderResolver.ToLlmConfig(profile), profile.Id, config, glossary)
        };
    }
}
