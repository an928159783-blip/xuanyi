namespace HoverTranslate.Core.Models;

/// <summary>翻译接口类型（均需用户自行填写 Key，应用不会上传密钥到炫译服务器）</summary>
public static class ApiProfileKinds
{
    public const string OpenAiCompatible = "openai";
    public const string Microsoft = "microsoft";
    public const string Google = "google";

    public static IReadOnlyList<(string Kind, string Label, string Hint)> Templates { get; } =
    [
        (OpenAiCompatible, "OpenAI 兼容（DeepSeek / 百炼 / 自建）",
            "填写兼容 OpenAI Chat Completions 的 Base URL、模型名与 API Key。"),
        (Microsoft, "微软翻译 (Azure Translator)",
            "在 Azure 门户创建「Translator」资源，使用订阅密钥与区域（如 eastasia）。"),
        (Google, "谷歌翻译 (Cloud Translation API)",
            "在 Google Cloud 启用 Cloud Translation API，使用 API Key。")
    ];
}
