using NeoTwitch.Models;

namespace NeoTwitch.Services.Alerts;

public sealed class AlertQueueService
{
    private readonly object _sync = new();
    private readonly TimeProvider _timeProvider;
    private readonly List<QueuedAlertSlot> _pendingSlots = [];
    private readonly Dictionary<string, DateTimeOffset> _lastRuleStartTimes = new(StringComparer.OrdinalIgnoreCase);
    private string _runningRuleId = "";
    private string _lastStartedRuleId = "";
    private DateTimeOffset _lastAlertStartAt = DateTimeOffset.MinValue;

    public AlertQueueService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public QueuedAlertSlot? TryReserve(
        EventRule rule,
        TwitchEvent twitchEvent,
        bool effectIsRunning,
        AlertQueueOptions options,
        out string reason)
    {
        lock (_sync)
        {
            var now = _timeProvider.GetUtcNow();
            var busy = effectIsRunning || _pendingSlots.Count > 0;
            var samePending = _pendingSlots.Count(slot => string.Equals(slot.RuleId, rule.Id, StringComparison.OrdinalIgnoreCase));

            if (busy)
            {
                if (samePending >= options.MaxQueuedSameRuleAlerts)
                {
                    reason = options.MaxQueuedSameRuleAlerts == 0
                        ? "No se permite acumular alertas repetidas."
                        : $"Ya hay {samePending} alerta(s) repetida(s) esperando.";
                    return null;
                }

                var isDifferentFromRunning = !string.IsNullOrWhiteSpace(_runningRuleId)
                    && !string.Equals(_runningRuleId, rule.Id, StringComparison.OrdinalIgnoreCase);
                var isDifferentWhileManualIsRunning = string.IsNullOrWhiteSpace(_runningRuleId)
                    && effectIsRunning;
                var isDifferentWhileQueueIsWaiting = string.IsNullOrWhiteSpace(_runningRuleId)
                    && _pendingSlots.Count > 0;
                var differentPending = _pendingSlots.Count(slot => !string.Equals(slot.RuleId, rule.Id, StringComparison.OrdinalIgnoreCase));

                if ((isDifferentFromRunning || isDifferentWhileManualIsRunning || isDifferentWhileQueueIsWaiting)
                    && differentPending >= options.MaxQueuedDifferentRuleAlerts)
                {
                    reason = options.MaxQueuedDifferentRuleAlerts == 0
                        ? "No se permite acumular alertas distintas mientras otra esta activa."
                        : $"Ya hay {differentPending} alerta(s) distinta(s) esperando.";
                    return null;
                }
            }

            if (options.SameRuleQueueCooldownMs > 0
                && _lastRuleStartTimes.TryGetValue(rule.Id, out var lastSameStart)
                && now - lastSameStart < TimeSpan.FromMilliseconds(options.SameRuleQueueCooldownMs))
            {
                var remainingMs = options.SameRuleQueueCooldownMs - (int)(now - lastSameStart).TotalMilliseconds;
                reason = $"Repetida en enfriamiento por {Math.Max(0, remainingMs)} ms.";
                return null;
            }

            if (options.DifferentRuleQueueCooldownMs > 0
                && !string.IsNullOrWhiteSpace(_lastStartedRuleId)
                && !string.Equals(_lastStartedRuleId, rule.Id, StringComparison.OrdinalIgnoreCase)
                && now - _lastAlertStartAt < TimeSpan.FromMilliseconds(options.DifferentRuleQueueCooldownMs))
            {
                var remainingMs = options.DifferentRuleQueueCooldownMs - (int)(now - _lastAlertStartAt).TotalMilliseconds;
                reason = $"Distinta en enfriamiento por {Math.Max(0, remainingMs)} ms.";
                return null;
            }

            var slot = new QueuedAlertSlot(Guid.NewGuid().ToString("N"), rule.Id, rule.Name, twitchEvent.Kind);
            _pendingSlots.Add(slot);
            reason = "";
            return slot;
        }
    }

    public void MarkStarted(QueuedAlertSlot? slot)
    {
        if (slot is null)
        {
            return;
        }

        lock (_sync)
        {
            _pendingSlots.RemoveAll(candidate => string.Equals(candidate.Id, slot.Id, StringComparison.OrdinalIgnoreCase));
            var now = _timeProvider.GetUtcNow();
            _runningRuleId = slot.RuleId;
            _lastStartedRuleId = slot.RuleId;
            _lastAlertStartAt = now;
            _lastRuleStartTimes[slot.RuleId] = now;
        }
    }

    public void MarkFinished(QueuedAlertSlot? slot)
    {
        if (slot is null)
        {
            return;
        }

        lock (_sync)
        {
            if (string.Equals(_runningRuleId, slot.RuleId, StringComparison.OrdinalIgnoreCase))
            {
                _runningRuleId = "";
            }
        }
    }
}
