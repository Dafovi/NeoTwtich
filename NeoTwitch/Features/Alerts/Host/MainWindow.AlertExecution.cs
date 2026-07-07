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

            obsRestore = await SendRuleObsSceneAsync(rule, effectCts.Token);
            _currentObsRestore = obsRestore;
            obsMediaHides = await SendRuleObsMediaAsync(rule, effectCts.Token);
            obsRestore = ObsRulePlanService.AlignSceneRestoreWithMedia(obsRestore, obsMediaHides);
            _currentObsRestore = obsRestore;

            _currentObsMediaHides.Clear();
            _currentObsMediaHides.AddRange(obsMediaHides);
            foreach (var obsMediaHide in obsMediaHides)
            {
                obsMediaHideTasks.Add(HideRuleObsMediaAfterDelayAsync(obsMediaHide, effectCts.Token));
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
                obsMediaHides.Count == 0 ? null : obsMediaHides.Max(media => media.Duration));

            var virtualLightsDuration = await StartRuleVirtualLightsAsync(
                rule,
                plan.SynchronizedDurationMs ?? rule.DurationMs,
                effectCts.Token);

            if (!plan.UseLights)
            {
                playback?.Play();
                await AlertEffectWaitService.WaitAsync(playback, null, obsMediaHides, effectCts.Token, virtualLightsDuration);
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
