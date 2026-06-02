namespace HoverTranslate.Core.Models;

/// <summary>设置页「+ 添加」可选的预设（仅百炼 / DeepSeek / 自定义）。</summary>
public static class ApiProfileAddPresets
{
    public sealed record Preset(string Id, string Label, string Kind, string DefaultName, string DefaultModel, string DefaultBaseUrl, string Hint);

    public static IReadOnlyList<Preset> All { get; } =
    [
        new("bailian", "阿里云百炼 · qwen-turbo", ApiProfileKinds.OpenAiCompatible,
            "qwen-turbo", "qwen-turbo", "https://dashscope.aliyuncs.com/compatible-mode/v1",
            "在百炼控制台创建 API Key；可按需改为 qwen-plus、qwen-long 等模型。"),
        new("deepseek", "DeepSeek · deepseek-chat", ApiProfileKinds.OpenAiCompatible,
            "deepseek-chat", "deepseek-chat", "https://api.deepseek.com",
            "在 DeepSeek 控制台创建 API Key，模型一般为 deepseek-chat。"),
        new("custom", "自定义", ApiProfileKinds.OpenAiCompatible,
            "my-model", "my-model", "https://api.example.com/v1",
            "填写兼容 OpenAI Chat Completions 的 Base URL、模型名与 API Key。")
    ];

    public static Preset Get(string? id) =>
        All.FirstOrDefault(p => p.Id == id) ?? All[0];
}
