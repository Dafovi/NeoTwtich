using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;
using NeoTwitch.ViewModels.Obs;
using static NeoTwitch.Services.Ui.UiBrushFactory;
using WpfMessageBox = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void ImageSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingUi || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        _imageSearchText = textBox.Text.Trim();
        RefreshMediaLibraryView(MediaLibraryKind.Image);
    }

    internal void VideoSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingUi || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        _videoSearchText = textBox.Text.Trim();
        RefreshMediaLibraryView(MediaLibraryKind.Video);
    }

    internal void VideoVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loadingUi)
        {
            return;
        }

        _config.VideoVolumePercent = (int)Math.Round(VideoVolumeSlider.Value);
        UpdateVideoVolumeText();
        SaveConfig();
    }

    internal void ImageFilterButton_Click(object sender, RoutedEventArgs e)
    {
        SetMediaFilter(MediaLibraryKind.Image, sender);
    }

    internal void VideoFilterButton_Click(object sender, RoutedEventArgs e)
    {
        SetMediaFilter(MediaLibraryKind.Video, sender);
    }

    private void SetMediaFilter(MediaLibraryKind kind, object sender)
    {
        if (sender is not System.Windows.Controls.Button button)
        {
            return;
        }

        if (kind == MediaLibraryKind.Image)
        {
            _imageFilter = button.Tag?.ToString() ?? "ALL";
            _imageGroupFilterId = "";
        }
        else
        {
            _videoFilter = button.Tag?.ToString() ?? "ALL";
            _videoGroupFilterId = "";
        }

        UpdateMediaFilterButtons(kind);
        RefreshMediaLibraryView(kind);
    }

    internal void ImageLibraryGroupBox_DropDownClosed(object sender, EventArgs e)
    {
        UpdateMediaAssetGroup(MediaLibraryKind.Image, sender);
    }

    internal void VideoLibraryGroupBox_DropDownClosed(object sender, EventArgs e)
    {
        UpdateMediaAssetGroup(MediaLibraryKind.Video, sender);
    }

    private void UpdateMediaAssetGroup(MediaLibraryKind kind, object sender)
    {
        if ((kind == MediaLibraryKind.Image && _refreshingImageLibrary)
            || (kind == MediaLibraryKind.Video && _refreshingVideoLibrary)
            || _loadingUi
            || sender is not System.Windows.Controls.ComboBox comboBox
            || comboBox.Tag is not string assetId)
        {
            return;
        }

        var library = GetMediaLibrary(kind);
        var asset = library.FirstOrDefault(item => string.Equals(item.Id, assetId, StringComparison.OrdinalIgnoreCase));
        if (asset is null)
        {
            return;
        }

        var selectedGroupId = comboBox.SelectedValue as string ?? "";
        if (string.Equals(asset.GroupId, selectedGroupId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        asset.GroupId = selectedGroupId;
        SaveConfig();
        _ = Dispatcher.InvokeAsync(() => RefreshMediaLibraryView(kind), DispatcherPriority.Background);
    }

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
            Filter = MediaLibraryKindCatalog.Get(kind).FileDialogFilter,
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

        SaveConfig();
        RefreshMediaLibraryView(kind);
        AddLog(_text.Format(UiTextKeys.LibrarySavedLog, title, asset.DisplayName), ActivityLogKind.Info);
    }

}
