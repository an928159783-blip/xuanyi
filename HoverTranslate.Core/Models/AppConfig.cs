namespace HoverTranslate.Core.Models;

public sealed class AppConfig
{
    /// <summary>auto | 某个 ApiProfile.Id</summary>
    public string Provider { get; set; } = "";
    /// <summary>Auto 模式下参与轮询的 API Id（空=全部已配置项）</summary>
    public List<string> AutoProviderIds { get; set; } = new();
    public string Hotkey { get; set; } = "Ctrl+Shift+T";
    /// <summary>auto | en-to-zh | zh-to-en（预留 extraLocalePairs 扩展更多语言对）</summary>
    public string TranslationDirection { get; set; } = "auto";
    /// <summary>复制到剪贴板后自动翻译（选中后只需 Ctrl+C）</summary>
    public bool TranslateOnCopy { get; set; }
    public bool EnableHistory { get; set; } = true;
    public bool EnableHover { get; set; } = true;
    public int HoverDelayMs { get; set; } = 500;
    /// <summary>悬停防抖下限（与 hoverDelayMs 取较大值），建议 300～500</summary>
    public int HoverMinDebounceMs { get; set; } = 400;
    /// <summary>悬停译文缓存 TTL（秒）</summary>
    public int HoverCacheTtlSeconds { get; set; } = 300;
    public int HoverCacheMaxEntries { get; set; } = 256;
    /// <summary>悬停每秒最多请求次数，超出丢弃</summary>
    public double HoverMaxRequestsPerSecond { get; set; } = 2.0;
    /// <summary>悬停遇限速时最多尝试次数（含首次）</summary>
    public int HoverRetryMaxAttempts { get; set; } = 2;
    public int HoverBackoffBaseMs { get; set; } = 2000;
    public int HoverBackoffMaxMs { get; set; } = 60_000;
    public bool UseHistoryPanel { get; set; } = true;
    public bool ShowPanelOnStartup { get; set; }
    public bool ShowPanelOnTranslate { get; set; } = true;
    /// <summary>设置窗口点击保存后自动关闭</summary>
    public bool CloseSettingsAfterSave { get; set; } = true;
    /// <summary>启动时显示「已启动」说明窗（托盘程序无主窗口）</summary>
    public bool ShowStartupNotice { get; set; } = true;
    public int OverlayTimeoutMs { get; set; }
    public bool Sanitize { get; set; } = true;
    public int MaxCharsPerRequest { get; set; } = 2000;
    public LlmProviderConfig Deepseek { get; set; } = new();
    public LlmProviderConfig Bailian { get; set; } = new();
    public List<ApiProfile> ApiProfiles { get; set; } = new();
    public PanelPlacement TranslationPanel { get; set; } = new();
    public PanelPlacement HistoryPanel { get; set; } = new();
    public PanelChromeOptions TranslationPanelUi { get; set; } = new();
    public PanelChromeOptions HistoryPanelUi { get; set; } = new();
    public Dictionary<string, string> Glossary { get; set; } = new();

    // 兼容旧版 config.json，Load 时迁移到 TranslationPanelUi / HistoryPanelUi
    public double OverlayOpacity { get; set; } = 0.9;
    public bool TranslationPanelShowExtras { get; set; }
    public double PanelScale { get; set; } = 1.0;
}

public sealed class LlmProviderConfig
{
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string Model { get; set; } = "";
}

public sealed class ApiProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    /// <summary>openai | microsoft | google</summary>
    public string Kind { get; set; } = ApiProfileKinds.OpenAiCompatible;
    public string ApiKey { get; set; } = "";
    /// <summary>OpenAI：Base URL；微软：可留空用官方端点</summary>
    public string BaseUrl { get; set; } = "";
    /// <summary>OpenAI：模型名；谷歌/微软可留空</summary>
    public string Model { get; set; } = "";
    /// <summary>微软翻译区域，如 eastasia、global</summary>
    public string Region { get; set; } = "global";
    public List<string> ModelChain { get; set; } = new();
    public int CurrentModelIndex { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class PanelPlacement
{
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
}

/// <summary>悬浮窗外观（译文框与历史框可分别配置）</summary>
public sealed class PanelChromeOptions
{
    public double Opacity { get; set; } = 0.92;
    /// <summary>dark | light | slate</summary>
    public string Theme { get; set; } = "dark";
    public bool ShowExtras { get; set; }
}
