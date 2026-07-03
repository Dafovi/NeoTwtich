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
    private async Task<ObsMediaHideRequest?> SendRuleObsMediaAsync(EventRule rule, CancellationToken cancellationToken)
    {
        var asset = ObsRulePlanService.ShouldSendMedia(rule, _config.Obs.IsConfigured)
            ? ResolveRuleObsMediaAsset(rule)
            : null;
        var plan = ObsRulePlanService.BuildMediaExecutionPlan(
            rule,
            _config,
            _obsService.CurrentScene,
            asset,
            NeoTwitchProduct.Obs.AlertImageSourceName,
            NeoTwitchProduct.Obs.AlertVideoSourceName);

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
                rule.ObsMediaKind,
                _config.Obs,
                plan.VolumePercent,
                cancellationToken);

            ApplyObsResult(result);
            WriteObsOverlayState(plan.Asset, rule.ObsMediaKind, plan.Duration);
            MarkObsMediaAssetUsed(rule.ObsMediaKind, plan.Asset);
            AddLog(_text.Format(UiTextKeys.ObsRuleMediaShownLog, plan.Asset.DisplayName, plan.SceneName), ActivityLogKind.Obs);

            return ObsRulePlanService.BuildMediaHideRequest(
                plan.SceneName,
                plan.SourceName,
                plan.Duration,
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
        AddLog(_text.Format(UiTextKeys.ObsRuleMediaHiddenLog, request.SceneName), ActivityLogKind.Obs);
    }
}
