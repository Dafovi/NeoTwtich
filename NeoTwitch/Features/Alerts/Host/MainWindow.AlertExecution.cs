using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Obs;

namespace NeoTwitch;

public partial class MainWindow
{
    private async Task RunRuleAsync(
        EventRule rule,
        TwitchEvent twitchEvent,
        bool sendChatMessage = true,
        bool sendAlexaEvent = true,
        QueuedAlertSlot? queueSlot = null)
    {
        await _effectGate.WaitAsync();
        _alertQueue.MarkStarted(queueSlot);
        var effectCts = new CancellationTokenSource();
        _currentEffectCts = effectCts;
        UpdateRuleTestButtonState();
        var wasCancelled = false;
        var shouldRestoreBackground = false;
        ObsSceneRestoreRequest? obsRestore = null;
        ObsMediaHideRequest? obsMediaHide = null;
        Task? obsMediaHideTask = null;
        _currentObsRestore = null;
        _currentObsMediaHide = null;
        _currentObsCleanedByStop = false;

        try
        {
            if (sendChatMessage)
            {
                _ = SendRuleChatMessageAsync(rule, twitchEvent);
            }

            if (sendAlexaEvent)
            {
                _ = SendRuleAlexaEventAsync(rule, twitchEvent);
            }

            obsRestore = await SendRuleObsSceneAsync(rule, effectCts.Token);
            _currentObsRestore = obsRestore;
            obsMediaHide = await SendRuleObsMediaAsync(rule, effectCts.Token);
            obsRestore = ObsRulePlanService.AlignSceneRestoreWithMedia(obsRestore, obsMediaHide);
            _currentObsRestore = obsRestore;

            _currentObsMediaHide = obsMediaHide;
            if (obsMediaHide is not null)
            {
                obsMediaHideTask = HideRuleObsMediaAfterDelayAsync(obsMediaHide, effectCts.Token);
            }

            AudioPlayback? playback = null;
            AudioAssetConfig? playbackAsset = null;
            if (rule.PlayAudio)
            {
                playbackAsset = ResolveRuleAudioAsset(rule);
                var audioPath = playbackAsset?.FilePath ?? rule.AudioPath;
                playback = await _audioPlayer.PrepareAsync(audioPath, _config.AlertVolumePercent, AddLog);
                _currentPlayback = playback;
                if (playbackAsset is not null)
                {
                    MarkAudioAssetUsed(playbackAsset, playback?.Duration);
                }
            }

            var plan = AlertExecutionPlanService.Build(
                rule,
                _config,
                _lightController.HasOpenPort,
                playback?.Duration,
                obsMediaHide?.Duration);

            if (!plan.UseLights)
            {
                playback?.Play();
                if (playback is not null)
                {
                    await playback.Completion.WaitAsync(effectCts.Token);
                }

                return;
            }

            if (plan.ShouldReconnectArduino)
            {
                await ConnectArduinoAsync();
            }

            shouldRestoreBackground = plan.ShouldRestoreBackground;
            await StopLightsAsync(plan.AllLightTargets);
            await Task.Delay(LightStopSettleMs);

            var command = plan.LightCommand;
            if (command is not null)
            {
                await _lightController.SendAsync(command, AddLog, CancellationToken.None);
                UpdateStatusText();
            }

            playback?.Play();
            await AlertEffectWaitService.WaitAsync(playback, command, obsMediaHide, effectCts.Token);

            if (command is not null)
            {
                await StopLightsAsync(plan.RuleLightTargets);
                AddLog($"Luces: {DisplayNameService.For(rule.Pattern, _text)} por {command.DurationMs} ms para {DisplayNameService.For(twitchEvent.Kind, _text)}.");
            }
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
            AddLog("Prueba detenida.");
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, $"Error ejecutando la regla '{rule.Name}'.");
            AddLog($"Regla '{rule.Name}': {ex.Message}");
        }
        finally
        {
            await CleanupRuleExecutionAsync(
                effectCts,
                queueSlot,
                shouldRestoreBackground,
                wasCancelled,
                obsRestore,
                obsMediaHide,
                obsMediaHideTask);
        }
    }

}
