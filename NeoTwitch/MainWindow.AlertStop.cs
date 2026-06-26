using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    private async Task StopCurrentEffectAsync()
    {
        _currentEffectCts?.Cancel();
        _currentPlayback?.Stop();
        await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
        await CleanupCurrentObsEffectAsync();
        await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOffEventName, "Fondo Alexa apagado");

        if (_effectGate.CurrentCount > 0)
        {
            await ApplyBackgroundStateAsync();
        }
    }

    private async Task CleanupCurrentObsEffectAsync()
    {
        var mediaHide = _currentObsMediaHide;
        var restore = _currentObsRestore;
        if (mediaHide is null && restore is null)
        {
            return;
        }

        try
        {
            if (mediaHide is not null)
            {
                await HideRuleObsMediaAsync(mediaHide, CancellationToken.None);
            }

            await RestoreRuleObsSceneAsync(restore, restoreImmediately: true);
            _currentObsCleanedByStop = true;
            _currentObsMediaHide = null;
            _currentObsRestore = null;
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo detener OBS durante la alerta.");
            AddLog($"OBS detener: {ex.Message}", ActivityLogKind.Important);
        }
    }
}
