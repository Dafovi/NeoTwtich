using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace NeoTwitch.Services.Ui;

public sealed record FilePickerRequest(
    string Title,
    string Filter,
    string? FileName = null,
    string? DefaultExtension = null,
    string? InitialDirectory = null,
    bool OverwritePrompt = false);

public sealed class FilePickerService
{
    public string? OpenFile(FilePickerRequest request)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = request.Title,
            Filter = request.Filter,
            CheckFileExists = true
        };

        if (!string.IsNullOrWhiteSpace(request.InitialDirectory))
        {
            dialog.InitialDirectory = request.InitialDirectory;
        }

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SaveFile(FilePickerRequest request)
    {
        var dialog = new WpfSaveFileDialog
        {
            Title = request.Title,
            Filter = request.Filter,
            FileName = request.FileName ?? "",
            AddExtension = !string.IsNullOrWhiteSpace(request.DefaultExtension),
            DefaultExt = request.DefaultExtension ?? "",
            OverwritePrompt = request.OverwritePrompt
        };

        if (!string.IsNullOrWhiteSpace(request.InitialDirectory))
        {
            dialog.InitialDirectory = request.InitialDirectory;
        }

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
