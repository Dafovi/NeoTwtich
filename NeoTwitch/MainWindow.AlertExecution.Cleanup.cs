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
        ObsMediaHideRequest? obsMediaHide,
        Task? obsMediaHideTask)
    {
        ClearCurrentPlayback(effectCts);
        await RestoreBackgroundAfterRuleAsync(shouldRestoreBackground, wasCancelled);
        await CleanupRuleObsMediaAsync(wasCancelled, obsMediaHide, obsMediaHideTask);

        if (!_currentObsCleanedByStop)
        {
            await RestoreRuleObsSceneAsync(obsRestore, wasCancelled);
        }

        ClearCurrentObsRuleState(obsRestore, obsMediaHide);
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
        ObsMediaHideRequest? obsMediaHide,
        Task? obsMediaHideTask)
    {
        if (_currentObsCleanedByStop || obsMediaHide is null)
        {
            return;
        }

        try
        {
            if (wasCancelled)
            {
                await HideRuleObsMediaAsync(obsMediaHide, CancellationToken.None);
            }
            else if (obsMediaHideTask is not null)
            {
                await obsMediaHideTask;
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
        ObsMediaHideRequest? obsMediaHide)
    {
        if (ReferenceEquals(_currentObsRestore, obsRestore))
        {
            _currentObsRestore = null;
        }

        if (ReferenceEquals(_currentObsMediaHide, obsMediaHide))
        {
            _currentObsMediaHide = null;
        }
    }
}
