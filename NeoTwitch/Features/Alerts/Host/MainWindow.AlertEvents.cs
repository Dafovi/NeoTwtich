using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Shell;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;
using Forms = System.Windows.Forms;

namespace NeoTwitch;

public partial class MainWindow
{
    private async void EventSubClient_EventReceived(TwitchEvent twitchEvent)
    {
        try
        {
            RegisterDashboardTwitchEvent(twitchEvent);
            var matchingRules = ResolveMatchingRules(twitchEvent);
            if (matchingRules.Length == 0)
            {
                if (twitchEvent.Kind != TwitchEventKind.ChatCommand)
                {
                    AddLog(twitchEvent.Title, ActivityLogKind.Event);
                    AddLog("El evento no coincide con alertas activas.");
                }

                return;
            }

            if (await TrySuppressOfflineTwitchAlertAsync(twitchEvent))
            {
                return;
            }

            AddLog(twitchEvent.Title, ActivityLogKind.Event);
            RegisterDashboardMatchedRules(matchingRules.Length);

            foreach (var rule in matchingRules)
            {
                await QueueAndRunRuleAsync(rule, twitchEvent);
            }
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, $"No se pudo procesar evento Twitch '{twitchEvent.Title}'.");
            AddLog($"Twitch evento: {ex.Message}", ActivityLogKind.Important);
        }
    }

    private async Task QueueAndRunRuleAsync(EventRule rule, TwitchEvent twitchEvent)
    {
        var slot = _alertQueue.TryReserve(
            rule,
            twitchEvent,
            _effectGate.CurrentCount == 0,
            AlertQueueOptions.FromConfig(_config),
            out var reason);
        if (slot is null)
        {
            AddLog($"Cola: descarte '{rule.Name}'. {reason}", ActivityLogKind.Important);
            return;
        }

        await RunRuleAsync(rule, twitchEvent, queueSlot: slot);
    }

    private EventRule[] ResolveMatchingRules(TwitchEvent twitchEvent)
    {
        return EventRuleMatcherService.ResolveMatches(_config.Rules, twitchEvent);
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
