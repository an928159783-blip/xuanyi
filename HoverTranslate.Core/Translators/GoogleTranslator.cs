using System.Text;
using System.Text.Json;
using HoverTranslate.Core.Http;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.Core.Translators;

/// <summary>Google Cloud Translation API v2</summary>
public sealed class GoogleTranslator : ITranslator
{
    private readonly ApiProfile _profile;
    private readonly AppConfig _config;
    private static HttpClient Http => TranslationHttpClient.Instance;

    public GoogleTranslator(ApiProfile profile, AppConfig config)
    {
        _profile = profile;
        _config = config;
    }

    public string ProviderName => _profile.Name;

    public async Task<TranslationResult> TranslateAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_profile.ApiKey))
            return TranslationResult.Fail(text, "未配置谷歌翻译 API Key。");

        var url = string.IsNullOrWhiteSpace(_profile.BaseUrl)
            ? $"https://translation.googleapis.com/language/translate/v2?key={Uri.EscapeDataString(_profile.ApiKey)}"
            : _profile.BaseUrl;

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        var (source, target) = TranslationDirectionResolver.ResolveGooglePair(text, _config);
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["q"] = text,
            ["source"] = source,
            ["target"] = target,
            ["format"] = "text"
        });

        try
        {
            using var resp = await Http.SendAsync(req, cancellationToken).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                return TranslationResult.Fail(text, $"谷歌翻译 HTTP {(int)resp.StatusCode}: {Trim(body, 200)}");

            using var doc = JsonDocument.Parse(body);
            var translated = doc.RootElement
                .GetProperty("data")
                .GetProperty("translations")[0]
                .GetProperty("translatedText")
                .GetString() ?? "";
            return TranslationResult.Ok(text, translated.Trim(), ProviderName);
        }
        catch (Exception ex)
        {
            return TranslationResult.Fail(text, $"谷歌翻译失败：{ex.Message}");
        }
    }

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
