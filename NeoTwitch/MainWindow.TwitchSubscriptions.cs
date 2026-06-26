using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    private string BuildEventSubscriptionSignature()
    {
        var activeKinds = _config.Rules
            .Where(rule => rule.IsEnabled)
            .Select(rule => rule.EventKind)
            .Where(kind => kind != TwitchEventKind.Test)
            .Distinct()
            .OrderBy(kind => kind)
            .Select(kind => kind.ToString());

        return string.Join("|", activeKinds);
    }

    private void ScheduleTwitchSubscriptionRefreshIfNeeded()
    {
        if (_initializingComponent || _loadingRule || !_eventSubClient.IsRunning)
        {
            return;
        }

        var signature = BuildEventSubscriptionSignature();
        if (string.Equals(signature, _eventSubscriptionSignature, StringComparison.Ordinal))
        {
            return;
        }

        _twitchSubscriptionRefreshDebounce?.Cancel();
        _twitchSubscriptionRefreshDebounce?.Dispose();

        var cts = new CancellationTokenSource();
        _twitchSubscriptionRefreshDebounce = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(900, cts.Token);
                var operation = Dispatcher.InvokeAsync(() => RefreshTwitchSubscriptionsAsync(signature));
                await await operation.Task;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                CrashReporter.Log(ex, "No se pudieron refrescar las suscripciones de Twitch.");
                AddLog($"Twitch: {ex.Message}", ActivityLogKind.Important);
                _ = Dispatcher.InvokeAsync(() =>
                {
                    _twitchConnectionError = ex.Message;
                    UpdateStatusText();
                });
            }
        });
    }

    private async Task RefreshTwitchSubscriptionsAsync(string signature)
    {
        if (!_eventSubClient.IsRunning)
        {
            _eventSubscriptionSignature = signature;
            return;
        }

        AddLog("Twitch: actualizando suscripciones por cambios en reglas.", ActivityLogKind.Twitch);
        await _eventSubClient.StopAsync();
        await _eventSubClient.StartAsync();
        _eventSubscriptionSignature = signature;
        _twitchConnectionError = "";
        AddLog("Twitch: suscripciones actualizadas.", ActivityLogKind.Twitch);
        UpdateStatusText();
    }
}
