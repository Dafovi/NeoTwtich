using System.IO;
using NeoTwitch.Models.Library;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    private async void RelinkMissingMediaFiles()
    {
        var assets = GetMediaLibraryAssets();
        var missingCount = assets.Count(asset => !File.Exists(asset.FilePath));
        if (missingCount == 0)
        {
            _dialog.ShowInformation(
                _text.Get(UiTextKeys.SettingsMediaRelinkTitle),
                _text.Get(UiTextKeys.SettingsMediaRelinkNoMissing));
            return;
        }

        var folder = _filePicker.OpenFolder(_text.Get(UiTextKeys.SettingsMediaRelinkPickFolder));
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        var result = await Task.Run(() => LibraryPathRelinkService.RelinkMissingAssets(assets, folder));
        if (result.RelinkedCount > 0)
        {
            SaveConfig();
            RefreshAudioLibraryView();
            RefreshMediaLibraryView(NeoTwitch.ViewModels.Library.MediaLibraryKind.Image);
            RefreshMediaLibraryView(NeoTwitch.ViewModels.Library.MediaLibraryKind.Video);
        }

        var message = _text.Format(
            UiTextKeys.SettingsMediaRelinkResult,
            result.RelinkedCount,
            result.UnresolvedCount,
            result.AmbiguousCount);
        AddLog(message, result.UnresolvedCount > 0 ? ActivityLogKind.Important : ActivityLogKind.Info);
        _dialog.ShowInformation(_text.Get(UiTextKeys.SettingsMediaRelinkTitle), message);
    }

    private ILibraryAssetConfig[] GetMediaLibraryAssets()
    {
        return _config.AudioLibrary.Cast<ILibraryAssetConfig>()
            .Concat(_config.ImageLibrary)
            .Concat(_config.VideoLibrary)
            .ToArray();
    }

    private int GetMissingMediaAssetCount()
    {
        return GetMediaLibraryAssets().Count(asset => !File.Exists(asset.FilePath));
    }
}
