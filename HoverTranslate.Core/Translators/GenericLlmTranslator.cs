using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.Core.Translators;

public sealed class GenericLlmTranslator : LlmTranslatorBase
{
    private readonly ApiProfile _profile;

    public GenericLlmTranslator(ApiProfile profile, AppConfig config, GlossaryService glossary)
        : base(ProviderResolver.ToLlmConfig(profile), profile.Id, config, glossary)
    {
        _profile = profile;
    }

    protected override string ResolveProviderLabel() => ApiProfileDisplay.GetPrimaryLabel(_profile);

    protected override string? ResolveModelName() =>
        string.IsNullOrWhiteSpace(_profile.Model) ? null : _profile.Model.Trim();
}
