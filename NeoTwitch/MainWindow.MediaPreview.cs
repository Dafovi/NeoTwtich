using System.IO;
using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;
using NeoTwitch.ViewModels.Obs;

namespace NeoTwitch;

public partial class MainWindow
{
    private async void PreviewMediaAsset(MediaLibraryKind kind, object? parameter)
    {
        await PreviewMediaAssetAsync(kind, parameter);
    }

    private async Task PreviewMediaAssetAsync(MediaLibraryKind kind, object? parameter)
    {
        if (parameter is not string assetId)
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
            _dialog.ShowInformation("OBS", _text.Get(UiTextKeys.MediaObsConnectRequiredPrompt));
            return;
        }

        var library = GetMediaLibrary(kind);
        var asset = library.FirstOrDefault(item => string.Equals(item.Id, assetId, StringComparison.OrdinalIgnoreCase));
        if (asset is null || !File.Exists(asset.FilePath))
        {
            AddLog(_text.Get(UiTextKeys.MediaObsMissingFileLog), ActivityLogKind.Important);
            return;
        }

        var plan = MediaPreviewPlanService.Build(kind, asset, _obsService.CurrentScene, _config.VideoVolumePercent);
        if (plan is null)
        {
            AddLog(_text.Get(UiTextKeys.MediaObsMissingSceneLog), ActivityLogKind.Important);
            return;
        }

        await StopMediaPreviewAsync();
        var previewCts = new CancellationTokenSource();
        _mediaPreviewCts = previewCts;
        _previewingMediaKind = kind;
        _previewingMediaId = asset.Id;
        RefreshMediaLibraryView(kind);

        try
        {
            var result = await _obsService.ShowMediaSourceAsync(
                plan.SceneName,
                plan.SourceName,
                asset.FilePath,
                plan.ObsKind,
                _config.Obs,
                plan.VolumePercent,
                previewCts.Token);
            ApplyObsResult(result);
            _mediaPreviewHideRequest = new ObsMediaHideRequest(plan.SceneName, plan.SourceName, plan.Duration, _timeProvider.GetUtcNow());
            WriteObsOverlayState(asset, plan.ObsKind, plan.Duration);
            MarkObsMediaAssetUsed(plan.ObsKind, asset);
            AddLog(_text.Format(UiTextKeys.MediaObsPreviewLog, MediaLibraryTitle(kind).ToLowerInvariant(), asset.DisplayName), ActivityLogKind.Obs);

            await Task.Delay(plan.Duration, previewCts.Token);
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
            _dialog.ShowWarning("OBS", ex.Message);
            await StopMediaPreviewAsync();
        }
    }

}
