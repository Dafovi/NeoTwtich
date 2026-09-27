using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Shell;
using NeoTwitch.Services.Streaming;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;
using Forms = System.Windows.Forms;

namespace NeoTwitch;

public partial class MainWindow
{
    private Task PlatformEventRouter_EventReceivedAsync(
        StreamEvent streamEvent,
        CancellationToken cancellationToken)
    {
        if (streamEvent.Platform is not (StreamingPlatform.Twitch or StreamingPlatform.YouTube))
        {
            return Task.CompletedTask;
        }

        return ProcessStreamEventAsync(streamEvent, cancellationToken);
    }

    private async Task ProcessStreamEventAsync(
        StreamEvent streamEvent,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var alertEvent = StreamEventRuleAdapter.ToAlertEvent(streamEvent);
            if (streamEvent.Kind == StreamEventKind.ChatCommand)
                IntegrationsView.ReceiveChat(alertEvent);
            RegisterDashboardStreamEvent(streamEvent);
            var logKind = streamEvent.Platform == StreamingPlatform.YouTube
                ? ActivityLogKind.YouTube
                : ActivityLogKind.Event;
            var matchingRules = EventRuleMatcherService.ResolveMatches(_config.Rules, streamEvent);
            if (matchingRules.Length == 0)
            {
                if (streamEvent.Kind != StreamEventKind.ChatCommand)
                {
                    AddLog(streamEvent.Title, logKind);
                    AddLog("El evento no coincide con alertas activas.");
                }

                return;
            }

            if (streamEvent.Platform == StreamingPlatform.Twitch
                && await TrySuppressOfflineTwitchAlertAsync(alertEvent))
            {
                return;
            }

            AddLog(streamEvent.Title, logKind);
            RegisterDashboardMatchedRules(matchingRules.Length);

            foreach (var rule in matchingRules)
            {
                await QueueAndRunRuleAsync(rule, alertEvent);
            }
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, $"No se pudo procesar evento {streamEvent.Platform} '{streamEvent.Title}'.");
            AddLog($"{streamEvent.Platform} evento: {ex.Message}", ActivityLogKind.Important);
        }
    }

    private async Task QueueAndRunRuleAsync(EventRule rule, TwitchEvent twitchEvent)
    {
        var slot = _alertQueue.TryReserve(
            rule,
            twitchEvent,
            _alertExecutionCoordinator.IsRunning,
            AlertQueueOptions.FromConfig(_config),
            out var reason);
        if (slot is null)
        {
            AddLog($"Cola: descarte '{rule.Name}'. {reason}", ActivityLogKind.Important);
            return;
        }

        await RunRuleAsync(rule, twitchEvent, queueSlot: slot);
    }

    private async Task<bool> TrySuppressOfflineTwitchAlertAsync(TwitchEvent twitchEvent)
    {
        var wasKnownOffline = _streamStatus is { IsLive: false };
        await RefreshTwitchStreamStatusForAlertGuardAsync();

        var isKnownOffline = _streamStatus is { IsLive: false }
            || wasKnownOffline && !string.IsNullOrWhiteSpace(_twitchConnectionError);
        if (!isKnownOffline)
        {
            return false;
        }

        AddLog(twitchEvent.Title, ActivityLogKind.Event);
        AddLog(_text.Format(UiTextKeys.AlertOfflineSuppressedLogFormat, twitchEvent.Title), ActivityLogKind.Important);
        ShowOfflineTwitchAlertNotification(twitchEvent);
        return true;
    }

    private async Task RefreshTwitchStreamStatusForAlertGuardAsync()
    {
        if (!_eventSubClient.IsRunning || !_config.Token.HasToken || !_config.Channel.IsReady)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (_streamStatus is not null && now - _lastStreamStatusRefreshAt < AlertStreamStatusRefreshInterval)
        {
            return;
        }

        await _streamStatusRefreshGate.WaitAsync();
        try
        {
            now = DateTimeOffset.UtcNow;
            if (_streamStatus is not null && now - _lastStreamStatusRefreshAt < AlertStreamStatusRefreshInterval)
            {
                return;
            }

            await RefreshTwitchStreamStatusAsync();
        }
        finally
        {
            _streamStatusRefreshGate.Release();
        }
    }

    private void ShowOfflineTwitchAlertNotification(TwitchEvent twitchEvent)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => ShowOfflineTwitchAlertNotification(twitchEvent));
            return;
        }

        if (_notifyIcon is null)
        {
            return;
        }

        TrayNotificationService.TryShowNotice(
            _notifyIcon,
            _text.Get(UiTextKeys.TrayOfflineAlertTitle),
            _text.Format(UiTextKeys.TrayOfflineAlertTextFormat, twitchEvent.Title),
            Forms.ToolTipIcon.Info,
            timeoutMs: 5000);
    }
}
