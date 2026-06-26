using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.ViewModels.Activity;

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
}
