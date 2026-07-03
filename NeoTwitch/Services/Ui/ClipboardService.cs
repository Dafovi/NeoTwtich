using WpfClipboard = System.Windows.Clipboard;

namespace NeoTwitch.Services.Ui;

public sealed class ClipboardService
{
    public void SetText(string text)
    {
        WpfClipboard.SetText(text);
    }
}
