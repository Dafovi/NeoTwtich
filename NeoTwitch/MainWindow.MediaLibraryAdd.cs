using System.IO;
using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;
using WpfMessageBox = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void BrowseNewImageButton_Click(object sender, RoutedEventArgs e)
    {
        BrowseNewMedia(MediaLibraryKind.Image);
    }

    internal void BrowseNewVideoButton_Click(object sender, RoutedEventArgs e)
    {
        BrowseNewMedia(MediaLibraryKind.Video);
    }

    private void BrowseNewMedia(MediaLibraryKind kind)
    {
        var dialog = new WpfOpenFileDialog
        {
            Filter = _text.Get(MediaLibraryKindCatalog.Get(kind).FileDialogFilterKey),
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        if (kind == MediaLibraryKind.Image)
        {
            _newImagePath = dialog.FileName;
            NewImagePathBox.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(NewImageNameBox.Text))
            {
                NewImageNameBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }
        else
        {
            _newVideoPath = dialog.FileName;
            NewVideoPathBox.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(NewVideoNameBox.Text))
            {
                NewVideoNameBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }
    }

    internal void SaveNewImageButton_Click(object sender, RoutedEventArgs e)
    {
        SaveNewMedia(MediaLibraryKind.Image);
    }

    internal void SaveNewVideoButton_Click(object sender, RoutedEventArgs e)
    {
        SaveNewMedia(MediaLibraryKind.Video);
    }

    private void SaveNewMedia(MediaLibraryKind kind)
    {
        var path = kind == MediaLibraryKind.Image ? _newImagePath : _newVideoPath;
        var title = MediaLibraryTitle(kind);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            WpfMessageBox.Show(this, _text.Format(UiTextKeys.MediaPickValidFile, title.ToLowerInvariant()), title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var library = GetMediaLibrary(kind);
        var existing = library.FirstOrDefault(asset => string.Equals(asset.FilePath, path, StringComparison.OrdinalIgnoreCase));
        var asset = existing ?? new MediaAssetConfig { FilePath = path };
        asset.Name = kind == MediaLibraryKind.Image
            ? string.IsNullOrWhiteSpace(NewImageNameBox.Text) ? Path.GetFileNameWithoutExtension(path) : NewImageNameBox.Text.Trim()
            : string.IsNullOrWhiteSpace(NewVideoNameBox.Text) ? Path.GetFileNameWithoutExtension(path) : NewVideoNameBox.Text.Trim();
        asset.GroupId = kind == MediaLibraryKind.Image
            ? NewImageGroupBox.SelectedValue as string ?? ""
            : NewVideoGroupBox.SelectedValue as string ?? "";

        if (kind == MediaLibraryKind.Image)
        {
            var size = MediaMetadataService.ProbeImageSize(path);
            asset.Width = size.Width;
            asset.Height = size.Height;
        }
        else
        {
            asset.DurationMs = MediaMetadataService.ProbeVideoDurationMs(path);
        }

        if (existing is null)
        {
            library.Add(asset);
        }

        ClearNewMediaForm(kind);

        SaveConfig();
        RefreshMediaLibraryView(kind);
        AddLog(_text.Format(UiTextKeys.LibrarySavedLog, title, asset.DisplayName), ActivityLogKind.Info);
    }

    private void ClearNewMediaForm(MediaLibraryKind kind)
    {
        if (kind == MediaLibraryKind.Image)
        {
            _newImagePath = "";
            NewImagePathBox.Text = "";
            NewImageNameBox.Text = "";
            NewImageGroupBox.SelectedValue = "";
        }
        else
        {
            _newVideoPath = "";
            NewVideoPathBox.Text = "";
            NewVideoNameBox.Text = "";
            NewVideoGroupBox.SelectedValue = "";
        }
    }
}
