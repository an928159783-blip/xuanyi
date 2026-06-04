namespace HoverTranslate.App.Services;

public sealed class ClipboardService
{
    public ClipboardBackup BackupClipboard()
    {
        var backup = new ClipboardBackup();
        backup.Capture();
        return backup;
    }

    public string? TryGetText()
    {
        try
        {
            if (!Clipboard.ContainsText()) return null;
            return Clipboard.GetText()?.Trim();
        }
        catch
        {
            return null;
        }
    }

    public void SetText(string text) => Clipboard.SetText(text);

    /// <summary>剪贴板含文件、图片等非纯文本时，不得 Clear/模拟复制，否则会破坏用户的文件复制。</summary>
    public bool HasPriorityNonTextContent()
    {
        try
        {
            var data = Clipboard.GetDataObject();
            if (data is null) return false;

            if (data.GetDataPresent(System.Windows.DataFormats.FileDrop, autoConvert: false))
                return true;
            if (data.GetDataPresent(System.Windows.DataFormats.Bitmap, autoConvert: false))
                return true;
            if (data.GetDataPresent(System.Windows.DataFormats.EnhancedMetafile, autoConvert: false))
                return true;
            if (data.GetDataPresent(System.Windows.DataFormats.WaveAudio, autoConvert: false))
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }

    public void Clear()
    {
        try
        {
            Clipboard.Clear();
        }
        catch
        {
            try { Clipboard.SetText(""); } catch { /* ignore */ }
        }
    }

    public sealed class ClipboardBackup : IDisposable
    {
        private System.Windows.IDataObject? _data;
        private bool _skipRestore;

        public void Capture()
        {
            try
            {
                _data = Clipboard.GetDataObject();
            }
            catch
            {
                _data = null;
            }
        }

        public void KeepNewContent() => _skipRestore = true;

        public void Dispose()
        {
            if (_skipRestore || _data is null) return;

            // copy:false — 立即写入快照并释放所有权，避免本进程长期占用剪贴板导致无法粘贴文件。
            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    Clipboard.SetDataObject(_data, copy: false);
                    return;
                }
                catch
                {
                    Thread.Sleep(25);
                }
            }
        }
    }
}
