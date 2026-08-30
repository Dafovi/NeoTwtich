using NeoTwitch.Models;

namespace NeoTwitch.Services.Alerts;

public sealed record AlertExecutionRequest(
    AlertExecutionRuleSnapshot Rule,
    AlertTriggerSnapshot Trigger,
    QueuedAlertSlot? QueueSlot,
    bool SendChatMessage,
    bool SendAlexaEvent);

public sealed record AlertExecutionResult(
    string ExecutionId,
    AlertExecutionState State,
    AlertExecutionTrace Trace)
{
    public bool IsCompleted => State == AlertExecutionState.Completed;
    public bool IsCancelled => State == AlertExecutionState.Cancelled;
    public bool IsFailed => State == AlertExecutionState.Failed;
}

public interface IAlertExecutionCapabilityState
{
}

public interface IAlertExecutionCapabilities
{
    IAlertExecutionCapabilityState CreateState();

    Task ExecuteChatAsync(AlertExecutionRequest request, CancellationToken cancellationToken);

    Task ExecuteAlexaAsync(AlertExecutionRequest request, CancellationToken cancellationToken);

    Task ExecuteEffectsAsync(
        AlertExecutionRequest request,
        AlertExecutionScope execution,
        IAlertExecutionCapabilityState state,
        CancellationToken cancellationToken);

    Task CleanupAsync(
        AlertExecutionRequest request,
        IAlertExecutionCapabilityState state,
        bool wasCancelled);
}

public sealed class AlertExecutionCoordinator : IDisposable
{
    private readonly object _sync = new();
    private readonly SemaphoreSlim _executionGate = new(1, 1);
    private readonly AlertExecutionTracker _tracker;
    private readonly AlertQueueService _queue;
    private CancellationTokenSource? _currentCancellation;
    private AlertExecutionScope? _currentExecution;
    private TaskCompletionSource? _currentCompletion;
    private int _disposed;

    public AlertExecutionCoordinator(AlertExecutionTracker tracker, AlertQueueService queue)
    {
        _tracker = tracker;
        _queue = queue;
    }

    public bool IsRunning => _executionGate.CurrentCount == 0;

    public string CurrentExecutionId
    {
        get
        {
            lock (_sync)
            {
                return _currentExecution?.Context.ExecutionId ?? "";
            }
        }
    }

    public async Task<AlertExecutionResult> ExecuteAsync(
        AlertExecutionRequest request,
        IAlertExecutionCapabilities capabilities,
        CancellationToken cancellationToken = default)
    {
        await _executionGate.WaitAsync(cancellationToken);
        _queue.MarkStarted(request.QueueSlot);
        using var executionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var queuedAt = request.QueueSlot?.QueuedAt ?? _tracker.TimeProvider.GetUtcNow();
        var execution = _tracker.Begin(
            request.Rule.RuleId,
            request.Rule.RuleName,
            request.Trigger.EventSubMessageId,
            request.QueueSlot?.Id ?? "",
            request.Trigger.Kind.ToString(),
            queuedAt,
            executionCancellation.Token);
        var state = capabilities.CreateState();
        var startedTasks = new List<Task>();
        var wasCancelled = false;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_sync)
        {
            _currentCancellation = executionCancellation;
            _currentExecution = execution;
            _currentCompletion = completion;
        }

        execution.MarkRunning();
        try
        {
            if (request.SendChatMessage && request.Rule.Chat.Enabled)
            {
                startedTasks.Add(RunOptionalActionAsync(
                    execution,
                    "TwitchChat",
                    token => capabilities.ExecuteChatAsync(request, token),
                    "Twitch chat request failed"));
            }

            if (request.SendAlexaEvent && request.Rule.Alexa.Enabled)
            {
                startedTasks.Add(RunOptionalActionAsync(
                    execution,
                    "Alexa",
                    token => capabilities.ExecuteAlexaAsync(request, token),
                    "Alexa request failed"));
            }

            var effectsTask = execution.RunActionAsync(
                "Effects",
                token => capabilities.ExecuteEffectsAsync(request, execution, state, token),
                "Core alert effects failed");
            startedTasks.Add(effectsTask);
            await Task.WhenAll(startedTasks);
            executionCancellation.Token.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) when (executionCancellation.IsCancellationRequested)
        {
            wasCancelled = true;
            execution.RequestCancellation("Execution cancellation requested");
        }
        catch (Exception ex)
        {
            execution.Fail($"Execution failed ({ex.GetType().Name})");
            executionCancellation.Cancel();
        }
        finally
        {
            await ObserveStartedTasksAsync(startedTasks, executionCancellation.Token);
            try
            {
                await capabilities.CleanupAsync(request, state, wasCancelled);
            }
            catch (Exception ex)
            {
                execution.Fail($"Cleanup failed ({ex.GetType().Name})");
            }

            execution.Finish(wasCancelled ? "Execution cancelled" : "Execution finished");
            lock (_sync)
            {
                if (ReferenceEquals(_currentExecution, execution))
                {
                    _currentExecution = null;
                    _currentCancellation = null;
                    _currentCompletion = null;
                }
            }

            _queue.MarkFinished(request.QueueSlot);
            _executionGate.Release();
            completion.TrySetResult();
        }

        var trace = execution.Trace;
        return new AlertExecutionResult(trace.Context.ExecutionId, trace.State, trace);
    }

    public bool CancelCurrent(string reason = "User requested stop")
    {
        lock (_sync)
        {
            if (_currentCancellation is null || _currentExecution is null)
            {
                return false;
            }

            _currentExecution.RequestCancellation(reason);
            _currentCancellation.Cancel();
            return true;
        }
    }

    public async Task<bool> StopAsync(TimeSpan timeout)
    {
        Task? completion;
        lock (_sync)
        {
            completion = _currentCompletion?.Task;
        }

        if (completion is null)
        {
            return true;
        }

        CancelCurrent("Application shutdown");
        try
        {
            await completion.WaitAsync(timeout);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _executionGate.Dispose();
        }
    }

    private static async Task RunOptionalActionAsync(
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
            throw;
        }
        catch
        {
            // Optional external actions do not prevent the remaining alert effects.
        }
    }

    private static async Task ObserveStartedTasksAsync(IEnumerable<Task> tasks, CancellationToken cancellationToken)
    {
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch
        {
            // Each task already recorded its own diagnostic before reaching this observer.
        }
    }
}
