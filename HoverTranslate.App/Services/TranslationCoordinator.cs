using System.Windows;
using HoverTranslate.App.Windows;
using HoverTranslate.Core.Models;
using HoverTranslate.Core.Services;

namespace HoverTranslate.App.Services;

public sealed class TranslationCoordinator
{
    private readonly ConfigService _configService;
    private readonly SelectionCaptureService _selection = new();
    private readonly TranslateOrchestrator _orchestrator = new();
    private readonly HoverProtectionService _hoverProtection = new();
    private HistoryStore? _history;
    private HoverTranslateService? _hover;

    public TranslationCoordinator(ConfigService configService)
    {
        _configService = configService;
    }

    public void AttachHover(HoverTranslateService hover) => _hover = hover;

    public Task TranslateFromSelectionOrClipboardAsync(string? preCaptured = null)
    {
        _hover?.OnManualTranslateStarting();

        return UiDispatcher.RunAsync(async () =>
        {
            try
            {
                await TranslateTextCoreAsync(preCaptured, fromHover: false, forceShowTranslation: true)
                    .ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ShowFatalError("翻译失败", ex);
            }
            finally
            {
                _hover?.OnManualTranslateFinished();
            }
        });
    }

    public Task TranslateTextAsync(string? text, bool fromHover, bool forceShowTranslation = false)
    {
        return UiDispatcher.RunAsync(async () =>
        {
            try
            {
                await TranslateTextCoreAsync(text, fromHover, forceShowTranslation).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                if (!fromHover)
                    ShowFatalError("翻译失败", ex);
            }
        });
    }

    private async Task TranslateTextCoreAsync(string? text, bool fromHover, bool forceShowTranslation)
    {
        var config = _configService.Load();
        text = text?.Trim() ?? "";

        if (!ConfigService.HasApiKey(config))
        {
            if (!fromHover)
            {
                ShowMessage(
                    "未配置翻译接口。请打开「设置」手动添加 API Key。\n密钥仅保存在本机 %USERPROFILE%\\.hover-translate\\，仅向您填写的服务商发送，炫译不会上传密钥。",
                    forceShowTranslation);
                SettingsWindowHost.ShowOrActivate();
            }
            return;
        }

        if (string.IsNullOrEmpty(text))
        {
            if (!fromHover)
                ShowMessage(
                    "未读到文字。请先选中文字并保持选中，再按 Ctrl+Shift+T；或先 Ctrl+C 复制，再按热键。",
                    forceShowTranslation);
            return;
        }

        var showTranslation = forceShowTranslation
            || config.ShowPanelOnTranslate
            || TranslationResultWindow.IsPanelVisible;

        if (showTranslation)
        {
            TranslationResultWindow.EnsureVisible();
            TranslationResultWindow.Instance.SetLoading(text);
        }

        TranslationResult result;
        if (fromHover)
        {
            var guardOptions = HoverProtectionOptions.FromConfig(config);
            var guarded = await _hoverProtection
                .TranslateAsync(text, guardOptions, ct => _orchestrator.TranslateAsync(text, config, fromHover: true, ct))
                .ConfigureAwait(true);

            if (guarded.SkipReason != HoverSkipReason.None)
                return;

            if (guarded.Result is null)
                return;

            result = guarded.Result;
        }
        else
        {
            result = await _orchestrator.TranslateAsync(text, config, fromHover: false).ConfigureAwait(true);
        }

        if (result.Success)
        {
            if (config.EnableHistory)
            {
                try
                {
                    _history ??= new HistoryStore();
                    _history.Add(result.SourceText, result.TranslatedText, result.Provider);
                }
                catch (Exception ex)
                {
                    if (showTranslation)
                        TranslationResultWindow.Instance.ShowError($"翻译成功，但写入历史失败：{ex.Message}");
                }
            }

            if (showTranslation)
                TranslationResultWindow.Instance.ShowCurrent(
                    result.SourceText, result.TranslatedText, result.Provider);

            if (HistoryPanelWindow.IsPanelVisible && config.EnableHistory && _history != null)
                HistoryPanelWindow.Instance.ReloadHistory(_history);
        }
        else if (showTranslation)
        {
            TranslationResultWindow.Instance.ShowError(result.ErrorMessage ?? "未知错误");
        }
    }

    private static void ShowMessage(string message, bool showTranslation)
    {
        if (!showTranslation) return;
        TranslationResultWindow.EnsureVisible();
        TranslationResultWindow.Instance.ShowError(message);
    }

    private static void ShowFatalError(string title, Exception ex)
    {
        TranslationResultWindow.EnsureVisible();
        TranslationResultWindow.Instance.ShowError($"{title}：{ex.Message}");
        System.Windows.MessageBox.Show(ex.ToString(), title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
