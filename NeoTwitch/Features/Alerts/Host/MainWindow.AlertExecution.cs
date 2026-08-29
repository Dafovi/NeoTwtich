using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Obs;

namespace NeoTwitch;

public partial class MainWindow
{
    private async Task RunRuleAsync(EventRule rule, TwitchEvent twitchEvent, bool sendChatMessage = true, bool sendAlexaEvent = true, QueuedAlertSlot? queueSlot = null)
    {
        var request = new AlertExecutionRequest(
            AlertExecutionSnapshotFactory.Create(rule),
            AlertExecutionSnapshotFactory.Create(twitchEvent),
            queueSlot,
            sendChatMessage,
            sendAlexaEvent);

        UpdateRuleTestButtonState();
        var result = await _alertExecutionCoordinator.ExecuteAsync(request, this);
        UpdateRuleTestButtonState();
        foreach (var action in result.Trace.Actions.Where(action => action.State == AlertActionState.Failed))
        {
            AddLog(
                $"Alerta [{result.ExecutionId[..Math.Min(8, result.ExecutionId.Length)]}] {action.ActionType}: fallo.",
                ActivityLogKind.Important);
        }

        AddLog(
            $"Alerta [{result.ExecutionId[..Math.Min(8, result.ExecutionId.Length)]}]: termina en estado {result.State}.",
            result.IsCompleted ? ActivityLogKind.Event : ActivityLogKind.Important);
    }

    IAlertExecutionCapabilityState IAlertExecutionCapabilities.CreateState()
    {
        _currentObsRestore = null;
        _currentObsMediaHides.Clear();
        _currentObsCleanedByStop = false;
        return new MainWindowAlertExecutionState();
    }

    Task IAlertExecutionCapabilities.ExecuteChatAsync(AlertExecutionRequest request, CancellationToken cancellationToken) =>
        SendRuleChatMessageAsync(request.Rule, request.Trigger, cancellationToken);

    Task IAlertExecutionCapabilities.ExecuteAlexaAsync(AlertExecutionRequest request, CancellationToken cancellationToken) =>
        SendRuleAlexaEventAsync(request.Rule, request.Trigger, cancellationToken);

    Task IAlertExecutionCapabilities.ExecuteEffectsAsync(AlertExecutionRequest request, AlertExecutionScope execution, IAlertExecutionCapabilityState state, CancellationToken cancellationToken) =>
        ExecuteAlertEffectsAsync(request, execution, (MainWindowAlertExecutionState)state, cancellationToken);

    Task IAlertExecutionCapabilities.CleanupAsync(AlertExecutionRequest request, IAlertExecutionCapabilityState state, bool wasCancelled) =>
        CleanupRuleExecutionAsync((MainWindowAlertExecutionState)state, wasCancelled);

    private async Task ExecuteAlertEffectsAsync(AlertExecutionRequest request, AlertExecutionScope execution, MainWindowAlertExecutionState state, CancellationToken cancellationToken)
    {
        var rule = request.Rule;
        var trigger = request.Trigger;
        AddLog($"Alerta [{execution.Context.ShortExecutionId}]: inicia regla '{rule.RuleName}'.", ActivityLogKind.Event);
        UpdateRuleTestButtonState();

        state.ObsSceneTask = RunObservedAlertActionAsync(execution, "OBS.Scene", token => SendRuleObsSceneAsync(rule, token), "OBS scene action failed", (ObsSceneRestoreRequest?)null);
        state.ObsMediaTask = RunObservedAlertActionAsync(execution, "OBS.Media", token => SendRuleObsMediaAsync(rule, token), "OBS media action failed", (IReadOnlyList<ObsMediaHideRequest>)[]);
        state.StartedTasks.Add(state.ObsSceneTask);
        state.StartedTasks.Add(state.ObsMediaTask);

        AudioPlayback? playback = null;
        if (rule.Audio.Enabled)
        {
            try
            {
                var playbackAsset = ResolveRuleAudioAsset(rule.Audio);
                var audioPath = playbackAsset?.FilePath ?? rule.Audio.LegacyPath;
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
                CrashReporter.Log(ex, $"No se pudo preparar el audio de la regla '{rule.RuleName}'.");
                AddLog($"Audio '{rule.RuleName}': {ex.Message}", ActivityLogKind.Important);
            }
        }

        var plan = AlertExecutionPlanService.Build(rule, _config, _lightController.HasOpenPort, playback?.Duration, null);
        if (!plan.UseLights)
        {
            playback?.Play();
        }
        else
        {
            if (plan.ShouldReconnectArduino)
            {
                await ConnectArduinoAsync(cancellationToken);
            }

            state.ShouldRestoreBackground = plan.ShouldRestoreBackground;
            await StopLightsAsync(plan.AllLightTargets, cancellationToken);
            await Task.Delay(LightStopSettleMs, cancellationToken);

            if (plan.LightCommand is not null)
            {
                var sent = await execution.RunActionAsync("Lights.Start", token => _lightController.SendAsync(plan.LightCommand, AddLog, token), "Light command failed");
                if (!sent && !cancellationToken.IsCancellationRequested)
                {
                    AddLog("Arduino: reconectando para reenviar la alerta.", ActivityLogKind.Important);
                    await ConnectArduinoAsync(cancellationToken);
                    await execution.RunActionAsync("Lights.Retry", token => _lightController.SendAsync(plan.LightCommand, AddLog, token), "Light retry failed");
                }

                UpdateStatusText();
            }

            playback?.Play();
        }

        state.ObsRestore = await state.ObsSceneTask;
        _currentObsRestore = state.ObsRestore;
        state.ObsMediaHides = await state.ObsMediaTask;
        state.ObsRestore = ObsRulePlanService.AlignSceneRestoreWithMedia(state.ObsRestore, state.ObsMediaHides);
        _currentObsRestore = state.ObsRestore;
        _currentObsMediaHides.Clear();
        _currentObsMediaHides.AddRange(state.ObsMediaHides);
        foreach (var mediaHide in state.ObsMediaHides)
        {
            state.ObsMediaHideTasks.Add(HideRuleObsMediaAfterDelayAsync(mediaHide, cancellationToken));
        }

        var virtualLightsDuration = await execution.RunActionAsync(
            "VirtualLights.Start",
            token => StartRuleVirtualLightsAsync(rule, plan.SynchronizedDurationMs, token),
            "Virtual lights action failed");
        cancellationToken.ThrowIfCancellationRequested();
        await execution.RunActionAsync(
            "Effects.Wait",
            token => AlertEffectWaitService.WaitAsync(playback, plan.LightCommand, state.ObsMediaHides, token, virtualLightsDuration),
            "Effect wait failed");

        if (plan.LightCommand is not null)
        {
            await StopLightsAsync(plan.RuleLightTargets, cancellationToken);
            AddLog($"Luces: {DisplayNameService.For(rule.Lights.Pattern, _text)} por {plan.LightCommand.DurationMs} ms para {DisplayNameService.For(trigger.Kind, _text)}.");
        }
    }

    private async Task<T> RunObservedAlertActionAsync<T>(AlertExecutionScope execution, string actionType, Func<CancellationToken, Task<T>> action, string failureReason, T fallback)
    {
        try
        {
            return await execution.RunActionAsync(actionType, action, failureReason);
        }
        catch (OperationCanceledException) when (execution.Context.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, $"Alerta {execution.Context.ExecutionId}: fallo la accion {actionType}.");
            AddLog($"Alerta [{execution.Context.ShortExecutionId}] {actionType}: fallo.", ActivityLogKind.Important);
            return fallback;
        }
    }

    private sealed class MainWindowAlertExecutionState : IAlertExecutionCapabilityState
    {
        public bool ShouldRestoreBackground { get; set; }
        public ObsSceneRestoreRequest? ObsRestore { get; set; }
        public IReadOnlyList<ObsMediaHideRequest> ObsMediaHides { get; set; } = [];
        public List<Task> ObsMediaHideTasks { get; } = [];
        public List<Task> StartedTasks { get; } = [];
        public Task<ObsSceneRestoreRequest?> ObsSceneTask { get; set; } = Task.FromResult<ObsSceneRestoreRequest?>(null);
        public Task<IReadOnlyList<ObsMediaHideRequest>> ObsMediaTask { get; set; } = Task.FromResult<IReadOnlyList<ObsMediaHideRequest>>([]);
    }
}
