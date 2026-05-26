using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.Core.Translators;

public sealed class DeepSeekTranslator(AppConfig config, GlossaryService glossary)
    : LlmTranslatorBase(config, glossary)
{
    protected override string ProviderId => "deepseek";
    protected override LlmProviderConfig GetConfig(AppConfig c) => c.Deepseek;
}
