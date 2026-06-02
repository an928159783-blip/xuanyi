using System.Drawing;
using HoverTranslate.App.Windows;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App.Services;

public sealed class ScreenshotTranslateService
{
    private readonly OcrService _ocr = new();
    private readonly TranslationCoordinator _coordinator;
    private readonly ConfigService _configService;
    private bool _busy;

    public ScreenshotTranslateService(TranslationCoordinator coordinator, ConfigService configService)
    {
        _coordinator = coordinator;
        _configService = configService;
    }

    public Task StartRegionPickAndTranslateAsync() =>
        UiDispatcher.RunAsync(async () => await RunRegionPickAndTranslateAsync().ConfigureAwait(true));

    private async Task RunRegionPickAndTranslateAsync()
    {
        if (_busy) return;
        _busy = true;
        _coordinator.BeginHoverSuppression();

        try
        {
            var config = _configService.Load();
            if (!ConfigService.HasApiKey(config))
            {
                AppDialog.Warning(
                    "未配置翻译接口。请先在设置中添加 API Key。",
                    AppBranding.SettingsTitle);
                SettingsWindowHost.ShowOrActivate();
                return;
            }

            var region = await RegionSelectorOverlayWindow.PickAsync().ConfigureAwait(true);
            if (region is null)
                return;

            await _ocr.InitializeAsync().ConfigureAwait(true);
            if (!_ocr.IsAvailable)
            {
                AppDialog.Warning(
                    "本机未安装或未启用 Windows OCR 语言包（建议安装英文识别）。",
                    AppBranding.DisplayName);
                return;
            }

            using var bitmap = ScreenCaptureService.Capture(region);
            var text = await _ocr.RecognizeAsync(bitmap).ConfigureAwait(true);
            if (string.IsNullOrWhiteSpace(text))
            {
                AppDialog.Warning("未识别到文字。请框选更清晰、更大的英文或中文区域。", AppBranding.DisplayName);
                return;
            }

            if (!TranslationDirectionResolver.ShouldTranslateText(text, config))
            {
                AppDialog.Info($"已识别文字，但当前翻译方向下不会翻译：\n{text[..Math.Min(text.Length, 120)]}...", AppBranding.DisplayName);
                return;
            }

            await _coordinator.TranslateTextAsync(text.Trim(), fromHover: false, forceShowTranslation: true)
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            AppDialog.Error($"截屏翻译失败：{ex.Message}", AppBranding.DisplayName);
        }
        finally
        {
            _busy = false;
            _coordinator.EndHoverSuppression();
        }
    }
}
