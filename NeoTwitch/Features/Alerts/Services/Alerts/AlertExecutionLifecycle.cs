namespace NeoTwitch.Services.Alerts;

public enum AlertExecutionState
{
    Starting,
    Running,
    Cancelling,
    Completed,
    Cancelled,
    Failed
}

public enum AlertActionState
{
    Running,
    Completed,
    Cancelled,
    Failed
}

public sealed record AlertExecutionContext(
    string ExecutionId,
    string RuleId,
    string RuleName,
    string EventMessageId,
    string QueueSlotId,
    string Source,
    DateTimeOffset QueuedAt,
    DateTimeOffset StartedAt,
    CancellationToken CancellationToken)
{
    public string ShortExecutionId => ExecutionId.Length <= 8 ? ExecutionId : ExecutionId[..8];
}

public sealed record AlertActionDiagnostic(
    string ExecutionId,
    string ActionId,
    string ActionType,
    AlertActionState State,
    DateTimeOffset StartedAt,
    TimeSpan Duration,
    string Reason);

public sealed record AlertExecutionTrace(
    AlertExecutionContext Context,
    AlertExecutionState State,
    DateTimeOffset? FinishedAt,
    TimeSpan Duration,
    string TerminalReason,
    IReadOnlyList<AlertActionDiagnostic> Actions);

public sealed class AlertExecutionTracker
{
    public const int DefaultMaxExecutions = 50;
    public const int DefaultMaxActionsPerExecution = 32;
    public const int MaxDiagnosticReasonLength = 256;

    private readonly object _sync = new();
    private readonly TimeProvider _timeProvider;
    private readonly Func<string> _idFactory;
    private readonly int _maxExecutions;
    private readonly int _maxActionsPerExecution;
    private readonly LinkedList<MutableTrace> _history = [];

    public AlertExecutionTracker(
        TimeProvider timeProvider,
        Func<string>? idFactory = null,
        int maxExecutions = DefaultMaxExecutions,
        int maxActionsPerExecution = DefaultMaxActionsPerExecution)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxExecutions, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxActionsPerExecution, 1);
        _timeProvider = timeProvider;
        _idFactory = idFactory ?? (() => Guid.NewGuid().ToString("N"));
        _maxExecutions = maxExecutions;
        _maxActionsPerExecution = maxActionsPerExecution;
    }

    public IReadOnlyList<AlertExecutionTrace> Recent
    {
        get
        {
            lock (_sync)
            {
                return _history.Reverse().Select(Snapshot).ToArray();
            }
        }
    }

    public AlertExecutionScope Begin(
        string ruleId,
        string ruleName,
        string eventMessageId,
        string queueSlotId,
        string source,
        DateTimeOffset queuedAt,
        CancellationToken cancellationToken)
    {
        var startedAt = _timeProvider.GetUtcNow();
        var context = new AlertExecutionContext(
            _idFactory(),
            ruleId,
            ruleName,
            eventMessageId,
            queueSlotId,
            source,
            queuedAt,
            startedAt,
            cancellationToken);
        var trace = new MutableTrace(context);

        lock (_sync)
        {
            _history.AddLast(trace);
            while (_history.Count > _maxExecutions)
            {
                _history.RemoveFirst();
            }
        }

        return new AlertExecutionScope(this, trace);
    }

    internal void MarkRunning(MutableTrace trace) => Transition(trace, AlertExecutionState.Running);

    internal void RequestCancellation(MutableTrace trace, string reason)
    {
        lock (_sync)
        {
            if (!IsTerminal(trace.State))
            {
                trace.State = AlertExecutionState.Cancelling;
                trace.TerminalReason = Bound(reason);
            }
        }
    }

    internal AlertActionToken StartAction(MutableTrace trace, string actionType)
    {
        trace.Context.CancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            var token = new AlertActionToken(
                $"{trace.Context.ExecutionId}:{++trace.ActionSequence}",
                actionType,
                _timeProvider.GetUtcNow());
            trace.Actions.Add(new AlertActionDiagnostic(
                trace.Context.ExecutionId,
                token.ActionId,
                actionType,
                AlertActionState.Running,
                token.StartedAt,
                TimeSpan.Zero,
                ""));
            TrimActions(trace);
            return token;
        }
    }

    internal void FinishAction(MutableTrace trace, AlertActionToken action, AlertActionState state, string reason)
    {
        lock (_sync)
        {
            var index = trace.Actions.FindIndex(item => item.ActionId == action.ActionId);
            if (index < 0)
            {
                return;
            }

            trace.Actions[index] = trace.Actions[index] with
            {
                State = state,
                Duration = NonNegative(_timeProvider.GetUtcNow() - action.StartedAt),
                Reason = Bound(reason)
            };
        }
    }

    internal void Finish(MutableTrace trace, string reason)
    {
        lock (_sync)
        {
            if (IsTerminal(trace.State))
            {
                return;
            }

            trace.State = trace.Context.CancellationToken.IsCancellationRequested
                || trace.State == AlertExecutionState.Cancelling
                ? AlertExecutionState.Cancelled
                : trace.Actions.Any(action => action.State == AlertActionState.Failed)
                    ? AlertExecutionState.Failed
                    : AlertExecutionState.Completed;
            trace.FinishedAt = _timeProvider.GetUtcNow();
            trace.TerminalReason = Bound(reason);
        }
    }

    internal void Fail(MutableTrace trace, string reason)
    {
        lock (_sync)
        {
            if (IsTerminal(trace.State))
            {
                return;
            }

            trace.State = AlertExecutionState.Failed;
            trace.FinishedAt = _timeProvider.GetUtcNow();
            trace.TerminalReason = Bound(reason);
        }
    }

    internal AlertExecutionTrace GetSnapshot(MutableTrace trace)
    {
        lock (_sync)
        {
            return Snapshot(trace);
        }
    }

    private void Transition(MutableTrace trace, AlertExecutionState state)
    {
        lock (_sync)
        {
            if (!IsTerminal(trace.State))
            {
                trace.State = state;
            }
        }
    }

    private void TrimActions(MutableTrace trace)
    {
        while (trace.Actions.Count > _maxActionsPerExecution)
        {
            trace.Actions.RemoveAt(0);
        }
    }

    private AlertExecutionTrace Snapshot(MutableTrace trace)
    {
        var now = trace.FinishedAt ?? _timeProvider.GetUtcNow();
        return new AlertExecutionTrace(
            trace.Context,
            trace.State,
            trace.FinishedAt,
            NonNegative(now - trace.Context.StartedAt),
            trace.TerminalReason,
            trace.Actions.ToArray());
    }

    private static bool IsTerminal(AlertExecutionState state) => state is
        AlertExecutionState.Completed or AlertExecutionState.Cancelled or AlertExecutionState.Failed;

    private static TimeSpan NonNegative(TimeSpan duration) => duration < TimeSpan.Zero ? TimeSpan.Zero : duration;

    private static string Bound(string value)
    {
        var normalized = (value ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();
        return normalized.Length <= MaxDiagnosticReasonLength
            ? normalized
            : normalized[..MaxDiagnosticReasonLength];
    }

    internal sealed class MutableTrace(AlertExecutionContext context)
    {
        public AlertExecutionContext Context { get; } = context;
        public AlertExecutionState State { get; set; } = AlertExecutionState.Starting;
        public DateTimeOffset? FinishedAt { get; set; }
        public string TerminalReason { get; set; } = "";
        public int ActionSequence { get; set; }
        public List<AlertActionDiagnostic> Actions { get; } = [];
    }

    internal sealed record AlertActionToken(string ActionId, string ActionType, DateTimeOffset StartedAt);
}

public sealed class AlertExecutionScope
{
    private readonly AlertExecutionTracker _tracker;
    private readonly AlertExecutionTracker.MutableTrace _trace;

    internal AlertExecutionScope(AlertExecutionTracker tracker, AlertExecutionTracker.MutableTrace trace)
    {
        _tracker = tracker;
        _trace = trace;
    }

    public AlertExecutionContext Context => _trace.Context;

    public AlertExecutionTrace Trace => _tracker.GetSnapshot(_trace);

    public void MarkRunning() => _tracker.MarkRunning(_trace);

    public void RequestCancellation(string reason = "Cancellation requested") =>
        _tracker.RequestCancellation(_trace, reason);

    public async Task RunActionAsync(
        string actionType,
        Func<CancellationToken, Task> action,
        string failureReason)
    {
        var actionToken = _tracker.StartAction(_trace, actionType);
        try
        {
            await action(Context.CancellationToken);
            _tracker.FinishAction(_trace, actionToken, AlertActionState.Completed, "");
        }
        catch (OperationCanceledException) when (Context.CancellationToken.IsCancellationRequested)
        {
            _tracker.FinishAction(_trace, actionToken, AlertActionState.Cancelled, "Cancellation requested");
            throw;
        }
        catch (Exception ex)
        {
            _tracker.FinishAction(
                _trace,
                actionToken,
                AlertActionState.Failed,
                $"{failureReason} ({ex.GetType().Name})");
            throw;
        }
    }

    public async Task<T> RunActionAsync<T>(
        string actionType,
        Func<CancellationToken, Task<T>> action,
        string failureReason)
    {
        T? result = default;
        await RunActionAsync(
            actionType,
            async cancellationToken => result = await action(cancellationToken),
            failureReason);
        return result!;
    }

    public void Finish(string reason = "Execution finished") => _tracker.Finish(_trace, reason);

    public void Fail(string reason) => _tracker.Fail(_trace, reason);
}
