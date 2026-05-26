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
            try
            {
                Clipboard.SetDataObject(_data, true);
            }
            catch
            {
                // ignore
            }
        }
    }
}
