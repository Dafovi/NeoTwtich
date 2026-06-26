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

}
