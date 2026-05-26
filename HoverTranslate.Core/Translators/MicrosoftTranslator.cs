using System.Text;
using System.Text.Json;
using HoverTranslate.Core.Http;
using HoverTranslate.Core.Models;

namespace HoverTranslate.Core.Translators;

/// <summary>Azure Translator Text API v3.0</summary>
public sealed class MicrosoftTranslator : ITranslator
{
    private readonly ApiProfile _profile;
    private static HttpClient Http => TranslationHttpClient.Instance;

    public MicrosoftTranslator(ApiProfile profile) => _profile = profile;

    public string ProviderName => _profile.Name;

    public async Task<TranslationResult> TranslateAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_profile.ApiKey))
            return TranslationResult.Fail(text, "未配置微软翻译 API Key。");

        var endpoint = string.IsNullOrWhiteSpace(_profile.BaseUrl)
            ? "https://api.cognitive.microsofttranslator.com"
            : _profile.BaseUrl.TrimEnd('/');

        var url = $"{endpoint}/translate?api-version=3.0&from=en&to=zh-Hans";
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
            using var resp = await Http.SendAsync(req, cancellationToken).ConfigureAwait(false);
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
