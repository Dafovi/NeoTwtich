using System.IO;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;
using WinFormsDialogResult = System.Windows.Forms.DialogResult;
using WinFormsFolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace NeoTwitch.Services.Ui;

public sealed record FilePickerRequest(
    string Title,
    string Filter,
    string? FileName = null,
    string? DefaultExtension = null,
    string? InitialDirectory = null,
    bool OverwritePrompt = false);

public interface IFilePickerService
{
    string? OpenFile(FilePickerRequest request);

    string? SaveFile(FilePickerRequest request);

    string? OpenFolder(string title, string? initialDirectory = null);
}

public sealed class FilePickerService : IFilePickerService
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

    public string? OpenFolder(string title, string? initialDirectory = null)
    {
        using var dialog = new WinFormsFolderBrowserDialog
        {
            Description = title,
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(initialDirectory) ? initialDirectory : ""
        };

        return dialog.ShowDialog() == WinFormsDialogResult.OK ? dialog.SelectedPath : null;
    }
}
