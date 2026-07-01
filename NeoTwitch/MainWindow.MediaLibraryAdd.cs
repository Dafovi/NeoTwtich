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
            _imageLibraryViewModel.SetNewAssetPath(dialog.FileName, Path.GetFileNameWithoutExtension(dialog.FileName));
        }
        else
        {
            _videoLibraryViewModel.SetNewAssetPath(dialog.FileName, Path.GetFileNameWithoutExtension(dialog.FileName));
        }
    }

    private void SaveNewMedia(MediaLibraryKind kind)
    {
        var viewModel = GetMediaLibraryViewModel(kind);
        var path = viewModel.NewAssetPath;
        var title = MediaLibraryTitle(kind);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            WpfMessageBox.Show(this, _text.Format(UiTextKeys.MediaPickValidFile, title.ToLowerInvariant()), title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var library = GetMediaLibrary(kind);
        var existing = library.FirstOrDefault(asset => string.Equals(asset.FilePath, path, StringComparison.OrdinalIgnoreCase));
        var asset = existing ?? new MediaAssetConfig { FilePath = path };
        asset.Name = string.IsNullOrWhiteSpace(viewModel.NewAssetName)
            ? Path.GetFileNameWithoutExtension(path)
            : viewModel.NewAssetName.Trim();
        asset.GroupId = viewModel.NewAssetGroupId;

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
        GetMediaLibraryViewModel(kind).ClearNewAssetForm();
    }
}
