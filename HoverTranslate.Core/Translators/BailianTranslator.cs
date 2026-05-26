using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.Core.Translators;

public sealed class BailianTranslator(AppConfig config, GlossaryService glossary)
    : LlmTranslatorBase(config, glossary)
{
    protected override string ProviderId => "bailian";
    protected override LlmProviderConfig GetConfig(AppConfig c) => c.Bailian;
}
