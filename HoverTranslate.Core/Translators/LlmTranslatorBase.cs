using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HoverTranslate.Core.Http;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.Core.Translators;

public abstract class LlmTranslatorBase : ITranslator
{
    private static HttpClient Http => TranslationHttpClient.Instance;

    private readonly AppConfig _config;
    private readonly GlossaryService _glossary;
    private readonly LlmProviderConfig? _fixedProfile;
    private readonly string? _fixedProviderId;

    protected LlmTranslatorBase(AppConfig config, GlossaryService glossary)
    {
        _config = config;
        _glossary = glossary;
    }

    protected LlmTranslatorBase(
        LlmProviderConfig profile,
        string providerId,
        AppConfig config,
        GlossaryService glossary)
    {
        _fixedProfile = profile;
        _fixedProviderId = providerId;
        _config = config;
        _glossary = glossary;
    }

    protected virtual string ProviderId =>
        _fixedProviderId ?? throw new InvalidOperationException("ProviderId not set");

    protected virtual LlmProviderConfig GetConfig(AppConfig config) =>
        _fixedProfile ?? throw new InvalidOperationException("Provider config not set");

    public string ProviderName => ProviderId;

    public async Task<TranslationResult> TranslateAsync(string text, CancellationToken cancellationToken = default)
    {
        var providerConfig = GetConfig(_config);
        if (string.IsNullOrWhiteSpace(providerConfig.ApiKey))
            return TranslationResult.Fail(text, $"{ProviderId} API Key 未配置。请写入 config.json 或设置环境变量。");

        if (text.Length > _config.MaxCharsPerRequest)
            return TranslationResult.Fail(text, $"文本超过 {_config.MaxCharsPerRequest} 字符，请缩短后重试。");

        var systemPrompt = BuildSystemPrompt(text);
        var url = $"{providerConfig.BaseUrl.TrimEnd('/')}/chat/completions";

        var payload = new
        {
            model = providerConfig.Model,
            temperature = 0.1,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = text }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", providerConfig.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return TranslationResult.Fail(text, $"{ProviderId} HTTP {(int)response.StatusCode}: {TrimForDisplay(body)}");

            var translated = ExtractAssistantContent(body);
            if (string.IsNullOrWhiteSpace(translated))
                return TranslationResult.Fail(text, $"{ProviderId} 返回为空。");

            translated = CleanModelOutput(translated);
            translated = _glossary.ApplyPostTranslation(translated, _config.Glossary);
            var (prompt, completion, total) = ExtractUsage(body);
            return TranslationResult.Ok(
                text, translated, ResolveProviderLabel(), ResolveModelName(),
                prompt, completion, total);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase)
                                              || ex.InnerException?.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase) == true)
        {
            return TranslationResult.Fail(text,
                $"{ProviderId} SSL 连接失败。请检查：1) 系统时间是否正确 2) 代理/VPN 3) 防火墙。详情：{TranslationHttpClient.FormatException(ex)}");
        }
        catch (Exception ex)
        {
            return TranslationResult.Fail(text, $"{ProviderId} 请求失败: {TranslationHttpClient.FormatException(ex)}");
        }
    }

    private string BuildSystemPrompt(string text)
    {
        var glossary = _glossary.BuildGlossaryPromptSection(_config.Glossary);
        return TranslationDirectionResolver.BuildLlmSystemPrompt(text, _config, glossary);
    }

    protected virtual string ResolveProviderLabel() => ProviderId;

    protected virtual string? ResolveModelName() => GetConfig(_config).Model;

    private static (int? Prompt, int? Completion, int? Total) ExtractUsage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("usage", out var usage))
                return (null, null, null);

            int? prompt = usage.TryGetProperty("prompt_tokens", out var p) ? p.GetInt32() : null;
            int? completion = usage.TryGetProperty("completion_tokens", out var c) ? c.GetInt32() : null;
            int? total = usage.TryGetProperty("total_tokens", out var t) ? t.GetInt32() : null;
            return (prompt, completion, total);
        }
        catch
        {
            return (null, null, null);
        }
    }

    private static string ExtractAssistantContent(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var choices = doc.RootElement.GetProperty("choices");
        if (choices.GetArrayLength() == 0) return "";
        return choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }

    private static string CleanModelOutput(string text)
    {
        text = text.Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var end = text.LastIndexOf("```", StringComparison.Ordinal);
            if (end > 3)
                text = text[3..end].Trim();
        }
        const string prefix = "译文：";
        if (text.StartsWith(prefix, StringComparison.Ordinal))
            text = text[prefix.Length..].Trim();
        return text;
    }

    private static string TrimForDisplay(string s) =>
        s.Length <= 200 ? s : s[..200] + "...";
}
