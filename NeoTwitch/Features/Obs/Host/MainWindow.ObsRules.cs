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
    private async Task<IReadOnlyList<ObsMediaHideRequest>> SendRuleObsMediaAsync(AlertExecutionRuleSnapshot rule, CancellationToken cancellationToken)
    {
        List<ObsMediaHideRequest> requests = [];

        var imageRequest = await SendRuleObsMediaAsync(
            rule,
            rule.Obs.Image,
            NeoTwitchProduct.Obs.AlertImageSourceName,
            cancellationToken);
        if (imageRequest is not null)
        {
            requests.Add(imageRequest);
        }

        var videoRequest = await SendRuleObsMediaAsync(
            rule,
            rule.Obs.Video,
            NeoTwitchProduct.Obs.AlertVideoSourceName,
            cancellationToken);
        if (videoRequest is not null)
        {
            requests.Add(videoRequest);
        }

        return requests;
    }

    private async Task<ObsMediaHideRequest?> SendRuleObsMediaAsync(
        AlertExecutionRuleSnapshot rule,
        AlertObsMediaActionSnapshot media,
        string sourceName,
        CancellationToken cancellationToken)
    {
        var asset = ObsRulePlanService.ShouldSendMedia(_config.Obs.IsConfigured, media.Enabled)
            ? ResolveRuleObsMediaAsset(media.Kind, media.Enabled, media.SourceMode, media.AssetId, media.GroupId)
            : null;
        var plan = ObsRulePlanService.BuildMediaExecutionPlan(
            rule,
            _config,
            _obsService.CurrentScene,
            asset,
            media.Enabled,
            media.Kind,
            media.DurationMs,
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
                await ConnectObsAsync(cancellationToken);
            }

            if (!_obsService.IsConnected)
            {
                return null;
            }

            var result = await _obsService.ShowMediaSourceAsync(
                plan.SceneName,
                plan.SourceName,
                plan.Asset!.FilePath,
                media.Kind,
                _config.Obs,
                plan.VolumePercent,
                cancellationToken);

            ApplyObsResult(result);
            WriteObsOverlayState(plan.Asset, media.Kind, plan.Duration);
            await MarkObsMediaAssetUsedAsync(media.Kind, plan.Asset);
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
            CrashReporter.Log(ex, $"No se pudo mostrar medio OBS para la regla '{rule.RuleName}'.");
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            UpdateObsStatusText();
            throw;
        }
    }

    private bool HandleObsMediaPlanStatus(AlertExecutionRuleSnapshot rule, ObsRuleMediaExecutionPlan plan)
    {
        switch (plan.Status)
        {
            case ObsRuleMediaPlanStatus.Disabled:
                return false;
            case ObsRuleMediaPlanStatus.MissingAsset:
                AddLog(_text.Format(UiTextKeys.ObsRuleMissingMediaLog, rule.RuleName), ActivityLogKind.Important);
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

    private async Task MarkObsMediaAssetUsedAsync(ObsMediaKind kind, MediaAssetConfig asset)
    {
        if (!Dispatcher.CheckAccess())
        {
            await Dispatcher.InvokeAsync(() => MarkObsMediaAssetUsed(kind, asset));
            return;
        }

        MarkObsMediaAssetUsed(kind, asset);
    }

    private void MarkObsMediaAssetUsed(ObsMediaKind kind, MediaAssetConfig asset)
    {
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

            await HideRuleObsMediaAsync(request, cancellationToken);
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
            await ConnectObsAsync(cancellationToken);
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
