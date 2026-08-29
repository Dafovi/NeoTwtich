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
        var execution = _alertExecutionTracker.Begin(
            rule.Id,
            rule.Name,
            twitchEvent.EventSubMessageId,
            queueSlot?.Id ?? "",
            twitchEvent.Kind.ToString(),
            queueSlot?.QueuedAt ?? _timeProvider.GetUtcNow(),
            effectCts.Token);
        _currentAlertExecution = execution;
        execution.MarkRunning();
        AddLog($"Alerta [{execution.Context.ShortExecutionId}]: inicia regla '{rule.Name}'.", ActivityLogKind.Event);
        UpdateRuleTestButtonState();
        var wasCancelled = false;
        var shouldRestoreBackground = false;
        ObsSceneRestoreRequest? obsRestore = null;
        IReadOnlyList<ObsMediaHideRequest> obsMediaHides = [];
        List<Task> obsMediaHideTasks = [];
        _currentObsRestore = null;
        _currentObsMediaHides.Clear();
        _currentObsCleanedByStop = false;
        List<Task> externalActionTasks = [];
        List<Task> startedActionTasks = [];

        try
        {
            if (sendChatMessage)
            {
                var chatTask = RunObservedAlertActionAsync(
                    execution,
                    "TwitchChat",
                    token => SendRuleChatMessageAsync(rule, twitchEvent, token),
                    "Twitch chat request failed");
                externalActionTasks.Add(chatTask);
                startedActionTasks.Add(chatTask);
            }

            if (sendAlexaEvent)
            {
                var alexaTask = RunObservedAlertActionAsync(
                    execution,
                    "Alexa",
                    token => SendRuleAlexaEventAsync(rule, twitchEvent, token),
                    "Alexa request failed");
                externalActionTasks.Add(alexaTask);
                startedActionTasks.Add(alexaTask);
            }

            // OBS can be reconnecting or unavailable. Start its work now, but never make
            // the serial command wait for it: local alerts must remain responsive.
            var obsSceneTask = RunObservedAlertActionAsync(
                execution,
                "OBS.Scene",
                token => SendRuleObsSceneAsync(rule, token),
                "OBS scene action failed",
                fallback: (ObsSceneRestoreRequest?)null);
            var obsMediaTask = RunObservedAlertActionAsync(
                execution,
                "OBS.Media",
                token => SendRuleObsMediaAsync(rule, token),
                "OBS media action failed",
                fallback: (IReadOnlyList<ObsMediaHideRequest>)[]);
            startedActionTasks.Add(obsSceneTask);
            startedActionTasks.Add(obsMediaTask);

            AudioPlayback? playback = null;
            AudioAssetConfig? playbackAsset = null;
            if (rule.PlayAudio)
            {
                try
                {
                    playbackAsset = ResolveRuleAudioAsset(rule);
                    var audioPath = playbackAsset?.FilePath ?? rule.AudioPath;
                    playback = await execution.RunActionAsync(
                        "Audio.Prepare",
                        _ => _audioPlayer.PrepareAsync(audioPath, _config.AlertVolumePercent, AddLog),
                        "Audio preparation failed");
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
                    await ConnectArduinoAsync(effectCts.Token);
                }

                shouldRestoreBackground = plan.ShouldRestoreBackground;
                await StopLightsAsync(plan.AllLightTargets, effectCts.Token);
                await Task.Delay(LightStopSettleMs, effectCts.Token);

                var ruleCommand = plan.LightCommand;
                if (ruleCommand is not null)
                {
                    var sent = await execution.RunActionAsync(
                        "Lights.Start",
                        token => _lightController.SendAsync(ruleCommand, AddLog, token),
                        "Light command failed");
                    if (!sent && !effectCts.IsCancellationRequested)
                    {
                        AddLog("Arduino: reconectando para reenviar la alerta.", ActivityLogKind.Important);
                        await ConnectArduinoAsync(effectCts.Token);
                        sent = await execution.RunActionAsync(
                            "Lights.Retry",
                            token => _lightController.SendAsync(ruleCommand, AddLog, token),
                            "Light retry failed");
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

            virtualLightsDuration = await execution.RunActionAsync(
                "VirtualLights.Start",
                token => StartRuleVirtualLightsAsync(rule, plan.SynchronizedDurationMs, token),
                "Virtual lights action failed");

            await Task.WhenAll(externalActionTasks);
            effectCts.Token.ThrowIfCancellationRequested();

            var command = plan.LightCommand;
            await execution.RunActionAsync(
                "Effects.Wait",
                token => AlertEffectWaitService.WaitAsync(playback, command, obsMediaHides, token, virtualLightsDuration),
                "Effect wait failed");

            if (command is not null)
            {
                await StopLightsAsync(plan.RuleLightTargets, effectCts.Token);
                AddLog($"Luces: {DisplayNameService.For(rule.Pattern, _text)} por {command.DurationMs} ms para {DisplayNameService.For(twitchEvent.Kind, _text)}.");
            }
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
            execution.RequestCancellation("User or host cancellation requested");
            AddLog($"Alerta [{execution.Context.ShortExecutionId}]: cancelada.", ActivityLogKind.Important);
        }
        catch (Exception ex)
        {
            execution.Fail($"Execution failed ({ex.GetType().Name})");
            effectCts.Cancel();
            CrashReporter.Log(ex, $"Error en alerta {execution.Context.ExecutionId} para la regla '{rule.Name}'.");
            AddLog($"Alerta [{execution.Context.ShortExecutionId}]: fallo la regla '{rule.Name}'.", ActivityLogKind.Important);
        }
        finally
        {
            try
            {
                await Task.WhenAll(startedActionTasks);
            }
            catch (OperationCanceledException) when (effectCts.IsCancellationRequested)
            {
                // Cancellation is recorded per action and on the execution trace.
            }
            catch (Exception ex)
            {
                execution.Fail($"Tracked action failed ({ex.GetType().Name})");
            }

            try
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
            catch (Exception ex)
            {
                execution.Fail($"Cleanup failed ({ex.GetType().Name})");
                CrashReporter.Log(ex, $"Fallo de limpieza en alerta {execution.Context.ExecutionId}.");
                AddLog($"Alerta [{execution.Context.ShortExecutionId}]: fallo la limpieza.", ActivityLogKind.Important);
            }
            finally
            {
                execution.Finish(wasCancelled ? "Execution cancelled" : "Execution finished");
                if (ReferenceEquals(_currentAlertExecution, execution))
                {
                    _currentAlertExecution = null;
                }

                AddLog(
                    $"Alerta [{execution.Context.ShortExecutionId}]: termina en estado {execution.Trace.State}.",
                    execution.Trace.State == AlertExecutionState.Completed
                        ? ActivityLogKind.Event
                        : ActivityLogKind.Important);
            }
        }
    }

    private async Task RunObservedAlertActionAsync(
        AlertExecutionScope execution,
        string actionType,
        Func<CancellationToken, Task> action,
        string failureReason)
    {
        try
        {
            await execution.RunActionAsync(actionType, action, failureReason);
        }
        catch (OperationCanceledException) when (execution.Context.CancellationToken.IsCancellationRequested)
        {
            AddLog($"Alerta [{execution.Context.ShortExecutionId}] {actionType}: cancelada.", ActivityLogKind.Important);
            throw;
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, $"Alerta {execution.Context.ExecutionId}: fallo la accion {actionType}.");
            AddLog($"Alerta [{execution.Context.ShortExecutionId}] {actionType}: fallo.", ActivityLogKind.Important);
        }
    }

    private async Task<T> RunObservedAlertActionAsync<T>(
        AlertExecutionScope execution,
        string actionType,
        Func<CancellationToken, Task<T>> action,
        string failureReason,
        T fallback)
    {
        try
        {
            return await execution.RunActionAsync(actionType, action, failureReason);
        }
        catch (OperationCanceledException) when (execution.Context.CancellationToken.IsCancellationRequested)
        {
            AddLog($"Alerta [{execution.Context.ShortExecutionId}] {actionType}: cancelada.", ActivityLogKind.Important);
            throw;
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, $"Alerta {execution.Context.ExecutionId}: fallo la accion {actionType}.");
            AddLog($"Alerta [{execution.Context.ShortExecutionId}] {actionType}: fallo.", ActivityLogKind.Important);
            return fallback;
        }
    }

}
