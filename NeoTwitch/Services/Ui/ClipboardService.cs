using WpfClipboard = System.Windows.Clipboard;

namespace NeoTwitch.Services.Ui;

public interface IClipboardService
{
    void SetText(string text);
}

public sealed class ClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        WpfClipboard.SetText(text);
    }
}
