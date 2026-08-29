using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Obs;

namespace NeoTwitch;

public partial class MainWindow
{
    private async Task<ObsSceneRestoreRequest?> SendRuleObsSceneAsync(AlertExecutionRuleSnapshot rule, CancellationToken cancellationToken)
    {
        if (!ObsRulePlanService.ShouldSendScene(rule, _config.Obs.IsConfigured))
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

            if (rule.Obs.Scene.DelayMs > 0)
            {
                await Task.Delay(rule.Obs.Scene.DelayMs, cancellationToken);
            }

            var previousScene = _obsService.CurrentScene;
            var targetScene = ObsRulePlanService.ResolveTargetScene(rule);
            var result = await _obsService.SetCurrentProgramSceneAsync(targetScene, cancellationToken);
            ApplyObsResult(result);
            AddLog(_text.Format(Services.Text.UiTextKeys.ObsRuleSceneSentLog, targetScene, rule.RuleName), ActivityLogKind.Obs);

            return ObsRulePlanService.BuildSceneRestoreRequest(
                rule,
                previousScene,
                targetScene,
                _timeProvider.GetUtcNow());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            CrashReporter.Log(ex, $"No se pudo enviar escena OBS para la regla '{rule.RuleName}'.");
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            UpdateObsStatusText();
            throw;
        }
    }

    private async Task RestoreRuleObsSceneAsync(ObsSceneRestoreRequest? restore, bool restoreImmediately)
    {
        if (restore is null)
        {
            return;
        }

        try
        {
            if (!restoreImmediately)
            {
                var remaining = restore.Delay - (_timeProvider.GetUtcNow() - restore.StartedAt);
                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining);
                }
            }

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

            var result = await _obsService.SetCurrentProgramSceneAsync(restore.PreviousScene, CancellationToken.None);
            ApplyObsResult(result);
            AddLog(_text.Format(Services.Text.UiTextKeys.ObsRuleSceneRestoredLog, restore.PreviousScene), ActivityLogKind.Obs);
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            CrashReporter.Log(ex, $"No se pudo restaurar la escena OBS '{restore.PreviousScene}'.");
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            UpdateObsStatusText();
        }
    }
}
