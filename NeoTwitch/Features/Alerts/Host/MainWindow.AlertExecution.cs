using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Text;
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
        IReadOnlyList<ObsMediaHideRequest> obsMediaHides = [];
        List<Task> obsMediaHideTasks = [];
        _currentObsRestore = null;
        _currentObsMediaHides.Clear();
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

            // OBS can be reconnecting or unavailable. Start its work now, but never make
            // the serial command wait for it: local alerts must remain responsive.
            var obsSceneTask = SendRuleObsSceneAsync(rule, effectCts.Token);
            var obsMediaTask = SendRuleObsMediaAsync(rule, effectCts.Token);

            AudioPlayback? playback = null;
            AudioAssetConfig? playbackAsset = null;
            if (rule.PlayAudio)
            {
                try
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
                catch (Exception ex)
                {
                    CrashReporter.Log(ex, $"No se pudo preparar el audio de la regla '{rule.Name}'.");
                    AddLog($"Audio '{rule.Name}': {ex.Message}", ActivityLogKind.Important);
                }
            }

            var plan = AlertExecutionPlanService.Build(
                rule,
                _config,
                _lightController.HasOpenPort,
                playback?.Duration,
                null);

            TimeSpan? virtualLightsDuration = null;

            if (!plan.UseLights)
            {
                playback?.Play();
            }
            else
            {
                if (plan.ShouldReconnectArduino)
                {
                    await ConnectArduinoAsync();
                }

                shouldRestoreBackground = plan.ShouldRestoreBackground;
                await StopLightsAsync(plan.AllLightTargets);
                await Task.Delay(LightStopSettleMs);

                var ruleCommand = plan.LightCommand;
                if (ruleCommand is not null)
                {
                    var sent = await _lightController.SendAsync(ruleCommand, AddLog, CancellationToken.None);
                    if (!sent && !effectCts.IsCancellationRequested)
                    {
                        AddLog("Arduino: reconectando para reenviar la alerta.", ActivityLogKind.Important);
                        await ConnectArduinoAsync();
                        sent = await _lightController.SendAsync(ruleCommand, AddLog, CancellationToken.None);
                    }

                    UpdateStatusText();
                }

                playback?.Play();
            }

            // Resolve OBS after local output has already started. This keeps a slow OBS
            // handshake from delaying Arduino, audio, or virtual screen effects.
            obsRestore = await obsSceneTask;
            _currentObsRestore = obsRestore;
            obsMediaHides = await obsMediaTask;
            obsRestore = ObsRulePlanService.AlignSceneRestoreWithMedia(obsRestore, obsMediaHides);
            _currentObsRestore = obsRestore;

            _currentObsMediaHides.Clear();
            _currentObsMediaHides.AddRange(obsMediaHides);
            foreach (var obsMediaHide in obsMediaHides)
            {
                obsMediaHideTasks.Add(HideRuleObsMediaAfterDelayAsync(obsMediaHide, effectCts.Token));
            }

            virtualLightsDuration = await StartRuleVirtualLightsAsync(
                rule,
                plan.SynchronizedDurationMs,
                effectCts.Token);

            var command = plan.LightCommand;
            await AlertEffectWaitService.WaitAsync(playback, command, obsMediaHides, effectCts.Token, virtualLightsDuration);

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
                obsMediaHides,
                obsMediaHideTasks);
        }
    }

}
