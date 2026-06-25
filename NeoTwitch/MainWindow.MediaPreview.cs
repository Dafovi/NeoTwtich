using System.IO;
using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;
using NeoTwitch.ViewModels.Obs;
using WpfMessageBox = System.Windows.MessageBox;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void DeleteImageButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteMediaAsset(MediaLibraryKind.Image, sender);
    }

    internal void DeleteVideoButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteMediaAsset(MediaLibraryKind.Video, sender);
    }

    internal async void PreviewImageButton_Click(object sender, RoutedEventArgs e)
    {
        await PreviewMediaAssetAsync(MediaLibraryKind.Image, sender);
    }

    internal async void PreviewVideoButton_Click(object sender, RoutedEventArgs e)
    {
        await PreviewMediaAssetAsync(MediaLibraryKind.Video, sender);
    }

    private async Task PreviewMediaAssetAsync(MediaLibraryKind kind, object sender)
    {
        if (sender is not FrameworkElement element || element.Tag is not string assetId)
        {
            return;
        }

        if (_previewingMediaKind == kind
            && string.Equals(_previewingMediaId, assetId, StringComparison.OrdinalIgnoreCase))
        {
            await StopMediaPreviewAsync();
            return;
        }

        if (!_config.Obs.IsConfigured || !_obsService.IsConnected)
        {
            AddLog(_text.Get(UiTextKeys.MediaObsConnectRequiredLog), ActivityLogKind.Important);
            WpfMessageBox.Show(this, _text.Get(UiTextKeys.MediaObsConnectRequiredPrompt), "OBS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var library = GetMediaLibrary(kind);
        var asset = library.FirstOrDefault(item => string.Equals(item.Id, assetId, StringComparison.OrdinalIgnoreCase));
        if (asset is null || !File.Exists(asset.FilePath))
        {
            AddLog(_text.Get(UiTextKeys.MediaObsMissingFileLog), ActivityLogKind.Important);
            return;
        }

        var sceneName = _obsService.CurrentScene;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            AddLog(_text.Get(UiTextKeys.MediaObsMissingSceneLog), ActivityLogKind.Important);
            return;
        }

        var info = MediaLibraryKindCatalog.Get(kind);
        var obsKind = info.ObsKind;
        var sourceName = info.PreviewSourceName;
        var duration = obsKind == ObsMediaKind.Video
            ? TimeSpan.FromMilliseconds(asset.DurationMs > 0 ? asset.DurationMs : 5000)
            : TimeSpan.FromSeconds(5);

        await StopMediaPreviewAsync();
        var previewCts = new CancellationTokenSource();
        _mediaPreviewCts = previewCts;
        _previewingMediaKind = kind;
        _previewingMediaId = asset.Id;
        RefreshMediaLibraryView(kind);

        try
        {
            var result = await _obsService.ShowMediaSourceAsync(
                sceneName,
                sourceName,
                asset.FilePath,
                obsKind,
                _config.Obs,
                obsKind == ObsMediaKind.Video ? _config.VideoVolumePercent : null,
                previewCts.Token);
            ApplyObsResult(result);
            _mediaPreviewHideRequest = new ObsMediaHideRequest(sceneName, sourceName, duration, DateTimeOffset.UtcNow);
            WriteObsOverlayState(asset, obsKind, duration);
            MarkObsMediaAssetUsed(obsKind, asset);
            AddLog(_text.Format(UiTextKeys.MediaObsPreviewLog, MediaLibraryTitle(kind).ToLowerInvariant(), asset.DisplayName), ActivityLogKind.Obs);

            await Task.Delay(duration, previewCts.Token);
            await StopMediaPreviewAsync();
        }
        catch (OperationCanceledException)
        {
            // The user stopped the preview.
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            CrashReporter.Log(ex, $"No se pudo probar medio OBS '{asset.DisplayName}'.");
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            UpdateObsStatusText();
            WpfMessageBox.Show(this, ex.Message, "OBS", MessageBoxButton.OK, MessageBoxImage.Warning);
            await StopMediaPreviewAsync();
        }
    }

    private async Task StopMediaPreviewAsync()
    {
        var cts = _mediaPreviewCts;
        var request = _mediaPreviewHideRequest;
        var kind = _previewingMediaKind;

        _mediaPreviewCts = null;
        _mediaPreviewHideRequest = null;
        _previewingMediaKind = null;
        _previewingMediaId = "";

        try
        {
            cts?.Cancel();
            if (request is not null && _obsService.IsConnected)
            {
                await HideRuleObsMediaAsync(request, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo detener la prueba de medio OBS.");
            AddLog($"OBS prueba: {ex.Message}", ActivityLogKind.Important);
        }
        finally
        {
            cts?.Dispose();
            if (kind is MediaLibraryKind mediaKind)
            {
                RefreshMediaLibraryView(mediaKind);
            }
        }
    }

    private void DeleteMediaAsset(MediaLibraryKind kind, object sender)
    {
        if (sender is not FrameworkElement element || element.Tag is not string assetId)
        {
            return;
        }

        var library = GetMediaLibrary(kind);
        var asset = library.FirstOrDefault(item => string.Equals(item.Id, assetId, StringComparison.OrdinalIgnoreCase));
        if (asset is null)
        {
            return;
        }

        var title = MediaLibraryTitle(kind);
        if (WpfMessageBox.Show(this, _text.Format(UiTextKeys.LibraryDeleteAssetPrompt, asset.DisplayName), title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        library.Remove(asset);
        SaveConfig();
        RefreshMediaLibraryView(kind);
    }
}
