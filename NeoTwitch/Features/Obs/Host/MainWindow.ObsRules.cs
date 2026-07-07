using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;
using NeoTwitch.ViewModels.Obs;
using static NeoTwitch.Services.Text.UiTextFormatter;

namespace NeoTwitch;

public partial class MainWindow
{
    private async Task<IReadOnlyList<ObsMediaHideRequest>> SendRuleObsMediaAsync(EventRule rule, CancellationToken cancellationToken)
    {
        List<ObsMediaHideRequest> requests = [];

        var imageRequest = await SendRuleObsMediaAsync(
            rule,
            ObsMediaKind.Image,
            rule.SendObsImage,
            rule.ObsImageSourceMode,
            rule.ObsImageAssetId,
            rule.ObsImageGroupId,
            rule.ObsImageDurationMs,
            NeoTwitchProduct.Obs.AlertImageSourceName,
            cancellationToken);
        if (imageRequest is not null)
        {
            requests.Add(imageRequest);
        }

        var videoRequest = await SendRuleObsMediaAsync(
            rule,
            ObsMediaKind.Video,
            rule.SendObsVideo,
            rule.ObsVideoSourceMode,
            rule.ObsVideoAssetId,
            rule.ObsVideoGroupId,
            rule.ObsMediaDurationMs,
            NeoTwitchProduct.Obs.AlertVideoSourceName,
            cancellationToken);
        if (videoRequest is not null)
        {
            requests.Add(videoRequest);
        }

        return requests;
    }

    private async Task<ObsMediaHideRequest?> SendRuleObsMediaAsync(
        EventRule rule,
        ObsMediaKind mediaKind,
        bool sendMedia,
        MediaSourceMode sourceMode,
        string assetId,
        string groupId,
        int durationMs,
        string sourceName,
        CancellationToken cancellationToken)
    {
        var asset = ObsRulePlanService.ShouldSendMedia(_config.Obs.IsConfigured, sendMedia)
            ? ResolveRuleObsMediaAsset(mediaKind, sendMedia, sourceMode, assetId, groupId)
            : null;
        var plan = ObsRulePlanService.BuildMediaExecutionPlan(
            rule,
            _config,
            _obsService.CurrentScene,
            asset,
            sendMedia,
            mediaKind,
            durationMs,
            sourceName,
            sourceName);

        if (!HandleObsMediaPlanStatus(rule, plan))
        {
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

            var result = await _obsService.ShowMediaSourceAsync(
                plan.SceneName,
                plan.SourceName,
                plan.Asset!.FilePath,
                mediaKind,
                _config.Obs,
                plan.VolumePercent,
                cancellationToken);

            ApplyObsResult(result);
            WriteObsOverlayState(plan.Asset, mediaKind, plan.Duration);
            MarkObsMediaAssetUsed(mediaKind, plan.Asset);
            AddLog(_text.Format(UiTextKeys.ObsRuleMediaShownLog, plan.Asset.DisplayName, plan.SceneName), ActivityLogKind.Obs);

            return ObsRulePlanService.BuildMediaHideRequest(
                plan.SceneName,
                plan.SourceName,
                plan.Duration,
                _timeProvider.GetUtcNow());
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

    private bool HandleObsMediaPlanStatus(EventRule rule, ObsRuleMediaExecutionPlan plan)
    {
        switch (plan.Status)
        {
            case ObsRuleMediaPlanStatus.Disabled:
                return false;
            case ObsRuleMediaPlanStatus.MissingAsset:
                AddLog(_text.Format(UiTextKeys.ObsRuleMissingMediaLog, rule.Name), ActivityLogKind.Important);
                return false;
            case ObsRuleMediaPlanStatus.MissingScene:
                AddLog(_text.Get(UiTextKeys.ObsRuleMissingSceneLog), ActivityLogKind.Important);
                return false;
            case ObsRuleMediaPlanStatus.Ready:
                return true;
            default:
                return false;
        }
    }

    private MediaAssetConfig? ResolveRuleObsMediaAsset(
        ObsMediaKind mediaKind,
        bool sendMedia,
        MediaSourceMode sourceMode,
        string assetId,
        string groupId)
    {
        if (!Dispatcher.CheckAccess())
        {
            return Dispatcher.Invoke(() => ResolveRuleObsMediaAsset(mediaKind, sendMedia, sourceMode, assetId, groupId));
        }

        return MediaRuleAssetService.ResolveRuleMediaAsset(
            sendMedia,
            mediaKind,
            sourceMode,
            assetId,
            groupId,
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

        LibraryAssetUsageService.MarkMediaUsed(asset, _timeProvider);
        SaveConfig();
        RefreshMediaLibraryView(kind == ObsMediaKind.Image ? MediaLibraryKind.Image : MediaLibraryKind.Video);
    }

    private async Task HideRuleObsMediaAfterDelayAsync(ObsMediaHideRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var remaining = request.Duration - (_timeProvider.GetUtcNow() - request.StartedAt);
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
        AddLog(_text.Format(UiTextKeys.ObsRuleMediaHiddenLog, request.SceneName), ActivityLogKind.Obs);
    }
}
