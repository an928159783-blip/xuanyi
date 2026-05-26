using System.Runtime.InteropServices;

namespace HoverTranslate.App.Services;

public sealed class SelectionCaptureService
{
    private readonly ClipboardService _clipboard = new();

    /// <summary>热键翻译：优先完整选区（须在 STA/UI 线程调用）。</summary>
    public string? GetTextForTranslation()
    {
        // 热键按下瞬间先读选区，避免松键后选区被折叠成单词
        var earlyUia = UiAutomationSelectionCapture.TryGetSelectedText();
        if (IsUsefulText(earlyUia))
            return earlyUia!.Trim();

        PrepareAfterHotkey();

        var fromUia = UiAutomationSelectionCapture.TryGetSelectedText();
        var fromCopy = TryCaptureSelectionViaCopy();
        var clip = _clipboard.TryGetText();

        return PickLongestUseful(earlyUia, fromUia, fromCopy, clip);
    }

    public string? CaptureSelectedText() => TryCaptureSelectionViaCopy();

    public string? GetClipboardText() => _clipboard.TryGetText();

    private static string? PickLongestUseful(params string?[] candidates)
    {
        string? best = null;
        foreach (var c in candidates)
        {
            if (!IsUsefulText(c)) continue;
            if (best is null || c!.Length > best.Length)
                best = c!.Trim();
        }
        return best;
    }

    private static void PrepareAfterHotkey()
    {
        ReleaseHotkeyKeys();
        Thread.Sleep(120);
    }

    private string? TryCaptureSelectionViaCopy()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd != IntPtr.Zero)
        {
            NativeMethods.SetForegroundWindow(hwnd);
            Thread.Sleep(50);

            var viaMessage = TryCopyMessage(hwnd);
            if (IsUsefulText(viaMessage))
                return viaMessage;
        }

        return TryCopyClearAndCtrlC();
    }

    private string? TryCopyClearAndCtrlC()
    {
        using var backup = _clipboard.BackupClipboard();
        _clipboard.Clear();
        Thread.Sleep(40);

        SendCtrlC();
        Thread.Sleep(450);

        var captured = _clipboard.TryGetText();
        if (IsUsefulText(captured))
        {
            backup.KeepNewContent();
            return captured;
        }

        backup.Dispose();
        return null;
    }

    private string? TryCopyMessage(IntPtr hwnd)
    {
        using var backup = _clipboard.BackupClipboard();
        _clipboard.Clear();
        Thread.Sleep(40);

        NativeMethods.PostMessage(hwnd, NativeMethods.WM_COPY, IntPtr.Zero, IntPtr.Zero);
        Thread.Sleep(260);

        var captured = _clipboard.TryGetText();
        if (IsUsefulText(captured))
        {
            backup.KeepNewContent();
            return captured;
        }

        backup.Dispose();
        return null;
    }

    private static bool IsUsefulText(string? text) =>
        !string.IsNullOrWhiteSpace(text) && text.Trim().Length >= 1;

    private static void ReleaseHotkeyKeys()
    {
        var keys = new List<NativeMethods.INPUT>();
        foreach (var vk in new ushort[]
                 {
                     NativeMethods.VkControl,
                     NativeMethods.VkShift,
                     NativeMethods.VkMenu,
                     (ushort)NativeMethods.VkT
                 })
        {
            if ((NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0)
                keys.Add(KeyUp(vk));
        }

        if (keys.Count == 0) return;
        NativeMethods.SendInput((uint)keys.Count, keys.ToArray(),
            Marshal.SizeOf<NativeMethods.INPUT>());
        Thread.Sleep(60);
    }

    private static void SendCtrlC()
    {
        var inputs = new[]
        {
            KeyDown(NativeMethods.VkControl),
            KeyDown(NativeMethods.VkC),
            KeyUp(NativeMethods.VkC),
            KeyUp(NativeMethods.VkControl)
        };
        NativeMethods.SendInput((uint)inputs.Length, inputs,
            Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static NativeMethods.INPUT KeyDown(ushort vk) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        U = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KEYBDINPUT { wVk = vk }
        }
    };

    private static NativeMethods.INPUT KeyUp(ushort vk) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        U = new NativeMethods.InputUnion
        {
            ki = new NativeMethods.KEYBDINPUT
            {
                wVk = vk,
                dwFlags = NativeMethods.KEYEVENTF_KEYUP
            }
        }
    };
}
