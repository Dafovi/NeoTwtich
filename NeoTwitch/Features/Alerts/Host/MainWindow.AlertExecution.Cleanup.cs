using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Obs;

namespace NeoTwitch;

public partial class MainWindow
{
    private async Task CleanupRuleExecutionAsync(
        CancellationTokenSource effectCts,
        QueuedAlertSlot? queueSlot,
        bool shouldRestoreBackground,
        bool wasCancelled,
        ObsSceneRestoreRequest? obsRestore,
        IReadOnlyCollection<ObsMediaHideRequest> obsMediaHides,
        IReadOnlyCollection<Task> obsMediaHideTasks)
    {
        ClearCurrentPlayback(effectCts);
        await RestoreBackgroundAfterRuleAsync(shouldRestoreBackground, wasCancelled);
        await CleanupRuleObsMediaAsync(wasCancelled, obsMediaHides, obsMediaHideTasks);

        if (!_currentObsCleanedByStop)
        {
            await RestoreRuleObsSceneAsync(obsRestore, wasCancelled);
        }

        ClearCurrentObsRuleState(obsRestore, obsMediaHides);
        effectCts.Dispose();
        _alertQueue.MarkFinished(queueSlot);
        _effectGate.Release();
    }

    private void ClearCurrentPlayback(CancellationTokenSource effectCts)
    {
        _currentPlayback = null;
        if (ReferenceEquals(_currentEffectCts, effectCts))
        {
            _currentEffectCts = null;
        }

        UpdateRuleTestButtonState();
    }

    private async Task RestoreBackgroundAfterRuleAsync(bool shouldRestoreBackground, bool wasCancelled)
    {
        if (!shouldRestoreBackground && !wasCancelled)
        {
            return;
        }

        try
        {
            await RestoreBackgroundStateAsync();
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo restaurar el fondo despues de una regla.");
            AddLog($"Fondo: {ex.Message}");
        }
    }

    private async Task CleanupRuleObsMediaAsync(
        bool wasCancelled,
        IReadOnlyCollection<ObsMediaHideRequest> obsMediaHides,
        IReadOnlyCollection<Task> obsMediaHideTasks)
    {
        if (_currentObsCleanedByStop || obsMediaHides.Count == 0)
        {
            return;
        }

        try
        {
            if (wasCancelled)
            {
                foreach (var obsMediaHide in obsMediaHides)
                {
                    await HideRuleObsMediaAsync(obsMediaHide, CancellationToken.None);
                }
            }
            else if (obsMediaHideTasks.Count > 0)
            {
                await Task.WhenAll(obsMediaHideTasks);
            }
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo ocultar el medio OBS despues de una regla.");
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
        }
    }

    private void ClearCurrentObsRuleState(
        ObsSceneRestoreRequest? obsRestore,
        IReadOnlyCollection<ObsMediaHideRequest> obsMediaHides)
    {
        if (ReferenceEquals(_currentObsRestore, obsRestore))
        {
            _currentObsRestore = null;
        }

        if (obsMediaHides.Count == 0 || _currentObsMediaHides.SequenceEqual(obsMediaHides))
        {
            _currentObsMediaHides.Clear();
        }
    }
}
