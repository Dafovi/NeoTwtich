using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Library;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;
using NeoTwitch.ViewModels.Obs;
using static NeoTwitch.Services.Text.UiTextFormatter;

namespace NeoTwitch;

public partial class MainWindow
{
    private async Task<ObsMediaHideRequest?> SendRuleObsMediaAsync(EventRule rule, CancellationToken cancellationToken)
    {
        if (!ObsRulePlanService.ShouldSendMedia(rule, _config.Obs.IsConfigured))
        {
            return null;
        }

        var asset = ResolveRuleObsMediaAsset(rule);
        if (asset is null)
        {
            AddLog($"OBS: la regla '{rule.Name}' no tiene un archivo valido para mostrar.", ActivityLogKind.Important);
            return null;
        }

        try
        {
            if (!_obsService.IsConnected)
            {
                await ConnectObsAsync();
            }

            if (!_obsService.IsConnected)
            {
                return null;
            }

            var sceneName = ObsRulePlanService.ResolveMediaSceneName(rule, _obsService.CurrentScene);

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                AddLog("OBS: no hay una escena actual para mostrar el medio.", ActivityLogKind.Important);
                return null;
            }

            var sourceName = ObsRulePlanService.ResolveAlertSourceName(
                rule.ObsMediaKind,
                NeoTwitchProduct.Obs.AlertImageSourceName,
                NeoTwitchProduct.Obs.AlertVideoSourceName);
            var mediaDuration = MediaRuleAssetService.ResolveRuleMediaDuration(rule, asset);
            var result = await _obsService.ShowMediaSourceAsync(
                sceneName,
                sourceName,
                asset.FilePath,
                rule.ObsMediaKind,
                _config.Obs,
                rule.ObsMediaKind == ObsMediaKind.Video ? _config.VideoVolumePercent : null,
                cancellationToken);

            ApplyObsResult(result);
            WriteObsOverlayState(asset, rule.ObsMediaKind, mediaDuration);
            MarkObsMediaAssetUsed(rule.ObsMediaKind, asset);
            AddLog($"OBS: medio '{asset.DisplayName}' mostrado en '{sceneName}'.", ActivityLogKind.Obs);

            return ObsRulePlanService.BuildMediaHideRequest(
                sceneName,
                sourceName,
                mediaDuration,
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            CrashReporter.Log(ex, $"No se pudo mostrar medio OBS para la regla '{rule.Name}'.");
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            UpdateObsStatusText();
            return null;
        }
    }

    private MediaAssetConfig? ResolveRuleObsMediaAsset(EventRule rule)
    {
        if (!Dispatcher.CheckAccess())
        {
            return Dispatcher.Invoke(() => ResolveRuleObsMediaAsset(rule));
        }

        return MediaRuleAssetService.ResolveRuleMediaAsset(
            rule,
            _config.ImageLibrary,
            _config.VideoLibrary,
            _previewRandom);
    }

    private void MarkObsMediaAssetUsed(ObsMediaKind kind, MediaAssetConfig asset)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => MarkObsMediaAssetUsed(kind, asset));
            return;
        }

        asset.LastUsedAt = DateTimeOffset.Now;
        SaveConfig();
        RefreshMediaLibraryView(kind == ObsMediaKind.Image ? MediaLibraryKind.Image : MediaLibraryKind.Video);
    }

    private async Task HideRuleObsMediaAfterDelayAsync(ObsMediaHideRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var remaining = request.Duration - (DateTimeOffset.UtcNow - request.StartedAt);
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, cancellationToken);
            }

            await HideRuleObsMediaAsync(request, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // The caller hides the media immediately when the alert is cancelled.
        }
    }

    private async Task HideRuleObsMediaAsync(ObsMediaHideRequest request, CancellationToken cancellationToken)
    {
        if (!_config.Obs.IsConfigured)
        {
            return;
        }

        if (!_obsService.IsConnected)
        {
            await ConnectObsAsync();
        }

        if (!_obsService.IsConnected)
        {
            return;
        }

        var result = await _obsService.HideSceneSourceAsync(
            request.SceneName,
            request.SourceName,
            cancellationToken);
        ApplyObsResult(result);
        ClearObsOverlayState();
        AddLog($"OBS: medio oculto en '{request.SceneName}'.", ActivityLogKind.Obs);
    }
}
