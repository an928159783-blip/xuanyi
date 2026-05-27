using System.Text;
using System.Text.Json;
using HoverTranslate.Core.Http;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.Core.Translators;

/// <summary>Azure Translator Text API v3.0</summary>
public sealed class MicrosoftTranslator : ITranslator
{
    private readonly ApiProfile _profile;
    private readonly AppConfig _config;
    private readonly HttpClient _http;

    public MicrosoftTranslator(ApiProfile profile, AppConfig config)
        : this(profile, config, TranslationHttpClient.Instance)
    {
    }

    internal MicrosoftTranslator(ApiProfile profile, AppConfig config, HttpClient http)
    {
        _profile = profile;
        _config = config;
        _http = http;
    }

    public string ProviderName => _profile.Name;

    public async Task<TranslationResult> TranslateAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_profile.ApiKey))
            return TranslationResult.Fail(text, "未配置微软翻译 API Key。");

        var endpoint = string.IsNullOrWhiteSpace(_profile.BaseUrl)
            ? "https://api.cognitive.microsofttranslator.com"
            : _profile.BaseUrl.TrimEnd('/');

        var (from, to) = TranslationDirectionResolver.ResolveApiPair(text, _config);
        var url = $"{endpoint}/translate?api-version=3.0&from={from}&to={to}";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("Ocp-Apim-Subscription-Key", _profile.ApiKey);
        if (!string.IsNullOrWhiteSpace(_profile.Region)
            && !_profile.Region.Equals("global", StringComparison.OrdinalIgnoreCase))
            req.Headers.Add("Ocp-Apim-Subscription-Region", _profile.Region);

        req.Content = new StringContent(
            JsonSerializer.Serialize(new[] { new { Text = text } }),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                return TranslationResult.Fail(text, $"微软翻译 HTTP {(int)resp.StatusCode}: {Trim(body, 200)}");

            using var doc = JsonDocument.Parse(body);
            var translated = doc.RootElement[0].GetProperty("translations")[0].GetProperty("text").GetString() ?? "";
            return TranslationResult.Ok(text, translated.Trim(), ProviderName);
        }
        catch (Exception ex)
        {
            return TranslationResult.Fail(text, $"微软翻译失败：{ex.Message}");
        }
    }

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
