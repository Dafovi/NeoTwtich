using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.ViewModels.Activity;
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

            var useLights = _config.ArduinoEnabled && rule.UseLights;

            if (!useLights)
            {
                playback?.Play();
                if (playback is not null)
                {
                    await playback.Completion.WaitAsync(effectCts.Token);
                }

                return;
            }

            if (useLights && !_lightController.HasOpenPort && !string.IsNullOrWhiteSpace(_config.SerialPort))
            {
                await ConnectArduinoAsync();
            }

            shouldRestoreBackground = true;
            var targets = LightCommand.ResolveTargets(_config, rule.TargetPins);
            if (useLights)
            {
                await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
                await Task.Delay(LightStopSettleMs);
            }

            var syncedDurationMs = AlertDurationService.ResolveSynchronizedEffectDurationMs(playback?.Duration, obsMediaHide?.Duration);

            LightCommand? command = null;
            if (useLights)
            {
                command = LightCommand.FromRule(rule, _config, syncedDurationMs);
                await _lightController.SendAsync(command, AddLog, CancellationToken.None);
                UpdateStatusText();
            }

            playback?.Play();
            await AlertEffectWaitService.WaitAsync(playback, command, obsMediaHide, effectCts.Token);

            if (command is not null)
            {
                await StopLightsAsync(targets);
                AddLog($"Luces: {DisplayNames.For(rule.Pattern)} por {command.DurationMs} ms para {DisplayNames.For(twitchEvent.Kind)}.");
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
            _currentPlayback = null;
            if (ReferenceEquals(_currentEffectCts, effectCts))
            {
                _currentEffectCts = null;
            }
            UpdateRuleTestButtonState();

            if (shouldRestoreBackground || wasCancelled)
            {
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

            if (!_currentObsCleanedByStop && obsMediaHide is not null)
            {
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

            if (!_currentObsCleanedByStop)
            {
                await RestoreRuleObsSceneAsync(obsRestore, wasCancelled);
            }

            if (ReferenceEquals(_currentObsRestore, obsRestore))
            {
                _currentObsRestore = null;
            }

            if (ReferenceEquals(_currentObsMediaHide, obsMediaHide))
            {
                _currentObsMediaHide = null;
            }

            effectCts.Dispose();
            _alertQueue.MarkFinished(queueSlot);
            _effectGate.Release();
        }
    }

}
