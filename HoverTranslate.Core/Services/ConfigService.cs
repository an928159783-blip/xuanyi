using System.Text.Json;
using HoverTranslate.Core.Models;

namespace HoverTranslate.Core.Services;

public sealed class ConfigService
{
    public static string ConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".hover-translate");

    public static string ConfigFilePath => Path.Combine(ConfigDirectory, "config.json");

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(ConfigDirectory);
        File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(config, JsonOptions));
    }

    /// <summary>合并保存，避免覆盖未加载的 API Key</summary>
    public void SaveMerged(Action<AppConfig> patch)
    {
        var merged = Load();
        patch(merged);
        Save(merged);
    }

    public static bool HasApiKey(AppConfig config) => ProviderResolver.HasApiKey(config);

    public AppConfig Load()
    {
        EnsureConfigExists();
        var json = File.ReadAllText(ConfigFilePath);
        var config = json.Contains("\"provider\"", StringComparison.OrdinalIgnoreCase)
            ? JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig()
            : LoadLegacyConfig(json);

        // 新版配置若未写 enableHistory，默认开启
        if (json.Contains("\"provider\"", StringComparison.OrdinalIgnoreCase)
            && !json.Contains("enableHistory", StringComparison.OrdinalIgnoreCase)
            && !json.Contains("EnableHistory", StringComparison.OrdinalIgnoreCase))
        {
            config.EnableHistory = true;
        }

        var migrated = MigratePanelUi(config);
        var profileCountBefore = config.ApiProfiles.Count;
        ConfigMigration.MigrateBuiltInToProfiles(config);
        if (migrated || config.ApiProfiles.Count > profileCountBefore)
            Save(config);
        return config;
    }

    /// <returns>是否修改了配置（需写回磁盘）</returns>
    public static bool MigratePanelUi(AppConfig config)
    {
        var changed = false;

        if (config.TranslationPanelUi.Opacity <= 0)
        {
            config.TranslationPanelUi.Opacity = 0.9;
            changed = true;
        }

        if (config.HistoryPanelUi.Opacity <= 0)
        {
            config.HistoryPanelUi.Opacity = 0.9;
            changed = true;
        }

        // 旧版误将 overlayOpacity（常为 0.6）写入 historyPanelUi
        if (config.OverlayOpacity is > 0 and < 0.85
            && Math.Abs(config.HistoryPanelUi.Opacity - config.OverlayOpacity) < 0.001)
        {
            config.HistoryPanelUi.Opacity = 0.9;
            changed = true;
        }

        if (!config.TranslationPanelUi.ShowExtras && config.TranslationPanelShowExtras)
            config.TranslationPanelUi.ShowExtras = true;

        var tOp = ClampPanelOpacity(config.TranslationPanelUi.Opacity);
        var hOp = ClampPanelOpacity(config.HistoryPanelUi.Opacity);
        if (Math.Abs(tOp - config.TranslationPanelUi.Opacity) > 0.0001) changed = true;
        if (Math.Abs(hOp - config.HistoryPanelUi.Opacity) > 0.0001) changed = true;
        config.TranslationPanelUi.Opacity = tOp;
        config.HistoryPanelUi.Opacity = hOp;

        var tScale = ClampFontSizeScale(config.TranslationPanelUi.FontSizeScale);
        var hScale = ClampFontSizeScale(config.HistoryPanelUi.FontSizeScale);
        if (Math.Abs(tScale - config.TranslationPanelUi.FontSizeScale) > 0.0001) changed = true;
        if (Math.Abs(hScale - config.HistoryPanelUi.FontSizeScale) > 0.0001) changed = true;
        config.TranslationPanelUi.FontSizeScale = tScale;
        config.HistoryPanelUi.FontSizeScale = hScale;

        return changed;
    }

    private static double ClampPanelOpacity(double value) => Math.Clamp(value, 0.0, 1.0);

    public static double ClampFontSizeScale(double value) => Math.Clamp(value, 0.90, 2.0);

    private static AppConfig LoadLegacyConfig(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var config = new AppConfig
        {
            Provider = "bailian",
            Sanitize = true,
            EnableHistory = !root.TryGetProperty("enable_history", out var eh) || eh.GetBoolean(),
            EnableHover = !root.TryGetProperty("enable_hover", out var hover) || hover.GetBoolean(),
            UseHistoryPanel = true,
            ShowPanelOnStartup = false,
            ShowPanelOnTranslate = true,
            HoverDelayMs = root.TryGetProperty("hover_delay_ms", out var hd) ? hd.GetInt32() : 800,
            OverlayTimeoutMs = root.TryGetProperty("overlay_timeout", out var ot) ? ot.GetInt32() : 0
        };

        if (root.TryGetProperty("api_key", out var apiKey))
            config.Bailian.ApiKey = apiKey.GetString() ?? "";
        if (root.TryGetProperty("base_url", out var baseUrl) && baseUrl.GetString() is { Length: > 0 } url)
            config.Bailian.BaseUrl = url;

        if (root.TryGetProperty("model_chain", out var chain) && chain.ValueKind == JsonValueKind.Array)
        {
            var idx = root.TryGetProperty("current_model_index", out var idxEl) ? idxEl.GetInt32() : 0;
            var len = chain.GetArrayLength();
            if (len > 0 && idx >= 0 && idx < len)
                config.Bailian.Model = chain[idx].GetString() ?? config.Bailian.Model;
        }

        config.Glossary = new Dictionary<string, string>
        {
            ["Agent"] = "智能体",
            ["User Rules"] = "用户规则"
        };
        return config;
    }

    public void EnsureConfigExists()
    {
        Directory.CreateDirectory(ConfigDirectory);
        if (File.Exists(ConfigFilePath))
            return;

        var example = Path.Combine(AppContext.BaseDirectory, "config.example.json");
        if (File.Exists(example))
        {
            File.Copy(example, ConfigFilePath);
            return;
        }

        var defaults = new AppConfig
        {
            Provider = "",
            Sanitize = true,
            EnableHistory = false,
            Glossary = new Dictionary<string, string>
            {
                ["Agent"] = "智能体",
                ["User Rules"] = "用户规则"
            }
        };
        File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(defaults, JsonOptions));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
