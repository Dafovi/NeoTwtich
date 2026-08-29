using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Text;

namespace NeoTwitch.Tests;

[TestClass]
public sealed class AlertExecutionLifecycleTests
{
    [TestMethod]
    public void ExecutionsReceiveUniqueIds()
    {
        var nextId = 0;
        var tracker = CreateTracker(idFactory: () => $"execution-{++nextId}");

        var first = Begin(tracker);
        var second = Begin(tracker);

        Assert.AreNotEqual(first.Context.ExecutionId, second.Context.ExecutionId);
    }

    [TestMethod]
    public async Task SuccessfulActionsReachCompleted()
    {
        var tracker = CreateTracker();
        var execution = Begin(tracker);
        execution.MarkRunning();

        await execution.RunActionAsync("Audio", _ => Task.CompletedTask, "Audio failed");
        execution.Finish();

        Assert.AreEqual(AlertExecutionState.Completed, execution.Trace.State);
        Assert.AreEqual(AlertActionState.Completed, execution.Trace.Actions.Single().State);
    }

    [TestMethod]
    public async Task CancellationReachesCancelled()
    {
        var tracker = CreateTracker();
        using var cancellation = new CancellationTokenSource();
        var execution = Begin(tracker, cancellation.Token);
        execution.MarkRunning();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var action = execution.RunActionAsync(
            "Delay",
            async token =>
            {
                started.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            },
            "Delay failed");
        await started.Task;
        execution.RequestCancellation();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => action);
        execution.Finish();
        Assert.AreEqual(AlertExecutionState.Cancelled, execution.Trace.State);
    }

    [TestMethod]
    public void ExecutionProducesOnlyOneTerminalState()
    {
        var execution = Begin(CreateTracker());
        execution.Finish("first");
        execution.RequestCancellation("late cancellation");
        execution.Fail("late failure");
        execution.Finish("second");

        Assert.AreEqual(AlertExecutionState.Completed, execution.Trace.State);
        Assert.AreEqual("first", execution.Trace.TerminalReason);
    }

    [TestMethod]
    public async Task CancellationStopsPendingDelay()
    {
        var tracker = CreateTracker();
        using var cancellation = new CancellationTokenSource();
        var execution = Begin(tracker, cancellation.Token);
        var action = execution.RunActionAsync(
            "PendingDelay",
            token => Task.Delay(TimeSpan.FromMinutes(1), token),
            "Delay failed");

        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => action);
        Assert.AreEqual(AlertActionState.Cancelled, execution.Trace.Actions.Single().State);
    }

    [TestMethod]
    public async Task ActionScheduledAfterCancellationDoesNotBegin()
    {
        var tracker = CreateTracker();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var execution = Begin(tracker, cancellation.Token);
        var began = false;

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => execution.RunActionAsync(
            "TooLate",
            _ =>
            {
                began = true;
                return Task.CompletedTask;
            },
            "Should not run"));

        Assert.IsFalse(began);
        Assert.AreEqual(0, execution.Trace.Actions.Count);
    }

    [TestMethod]
    public async Task CompletedActionIsNotUndoneByLaterCancellation()
    {
        var tracker = CreateTracker();
        using var cancellation = new CancellationTokenSource();
        var execution = Begin(tracker, cancellation.Token);
        await execution.RunActionAsync("Chat", _ => Task.CompletedTask, "Chat failed");

        execution.RequestCancellation();
        cancellation.Cancel();
        execution.Finish();

        Assert.AreEqual(AlertActionState.Completed, execution.Trace.Actions.Single().State);
        Assert.AreEqual(AlertExecutionState.Cancelled, execution.Trace.State);
    }

    [TestMethod]
    public async Task ActionDiagnosticsCarryExecutionId()
    {
        var execution = Begin(CreateTracker());
        await execution.RunActionAsync("Alexa", _ => Task.CompletedTask, "Alexa failed");

        Assert.AreEqual(execution.Context.ExecutionId, execution.Trace.Actions.Single().ExecutionId);
    }

    [TestMethod]
    public async Task ConcurrentExecutionsDoNotMixActions()
    {
        var tracker = CreateTracker();
        var first = Begin(tracker);
        var second = Begin(tracker);

        await Task.WhenAll(
            first.RunActionAsync("FirstOnly", _ => Task.CompletedTask, "failed"),
            second.RunActionAsync("SecondOnly", _ => Task.CompletedTask, "failed"));

        CollectionAssert.AreEqual(new[] { "FirstOnly" }, first.Trace.Actions.Select(action => action.ActionType).ToArray());
        CollectionAssert.AreEqual(new[] { "SecondOnly" }, second.Trace.Actions.Select(action => action.ActionType).ToArray());
    }

    [TestMethod]
    public void RetainedExecutionHistoryIsBounded()
    {
        var tracker = CreateTracker(maxExecutions: 2);
        var first = Begin(tracker);
        var second = Begin(tracker);
        var third = Begin(tracker);

        Assert.AreEqual(2, tracker.Recent.Count);
        Assert.IsFalse(tracker.Recent.Any(trace => trace.Context.ExecutionId == first.Context.ExecutionId));
        Assert.IsTrue(tracker.Recent.Any(trace => trace.Context.ExecutionId == second.Context.ExecutionId));
        Assert.IsTrue(tracker.Recent.Any(trace => trace.Context.ExecutionId == third.Context.ExecutionId));
    }

    [TestMethod]
    public async Task ActionDiagnosticHistoryIsBounded()
    {
        var tracker = CreateTracker(maxActions: 2);
        var execution = Begin(tracker);

        await execution.RunActionAsync("One", _ => Task.CompletedTask, "failed");
        await execution.RunActionAsync("Two", _ => Task.CompletedTask, "failed");
        await execution.RunActionAsync("Three", _ => Task.CompletedTask, "failed");

        CollectionAssert.AreEqual(
            new[] { "Two", "Three" },
            execution.Trace.Actions.Select(action => action.ActionType).ToArray());
    }

    [TestMethod]
    public async Task ActionExceptionIsCapturedAndExecutionFails()
    {
        var execution = Begin(CreateTracker());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => execution.RunActionAsync(
            "Chat",
            _ => throw new InvalidOperationException("remote detail"),
            "Chat request failed"));
        execution.Finish();

        Assert.AreEqual(AlertActionState.Failed, execution.Trace.Actions.Single().State);
        Assert.AreEqual(AlertExecutionState.Failed, execution.Trace.State);
        Assert.IsFalse(execution.Trace.Actions.Single().Reason.Contains("remote detail", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task FailingActionTaskIsAwaitedAndObserved()
    {
        var execution = Begin(CreateTracker());
        var task = execution.RunActionAsync(
            "ObservedFailure",
            _ => throw new HttpRequestException("failure"),
            "Observed request failed");

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => task);

        Assert.IsTrue(task.IsFaulted);
        Assert.AreEqual(AlertActionState.Failed, execution.Trace.Actions.Single().State);
    }

    [TestMethod]
    public async Task DurationsUseInjectedTimeProvider()
    {
        var time = new ManualAlertTimeProvider();
        var tracker = new AlertExecutionTracker(time);
        var execution = Begin(tracker);

        await execution.RunActionAsync(
            "Timed",
            _ =>
            {
                time.Advance(TimeSpan.FromSeconds(3));
                return Task.CompletedTask;
            },
            "failed");
        time.Advance(TimeSpan.FromSeconds(2));
        execution.Finish();

        Assert.AreEqual(TimeSpan.FromSeconds(3), execution.Trace.Actions.Single().Duration);
        Assert.AreEqual(TimeSpan.FromSeconds(5), execution.Trace.Duration);
    }

    [TestMethod]
    public async Task DiagnosticReasonsAreBounded()
    {
        var execution = Begin(CreateTracker());
        var reason = new string('x', 1000);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => execution.RunActionAsync(
            "Bounded",
            _ => throw new InvalidOperationException(),
            reason));

        Assert.AreEqual(AlertExecutionTracker.MaxDiagnosticReasonLength, execution.Trace.Actions.Single().Reason.Length);
    }

    [TestMethod]
    public void ContextPreservesQueueAndUpstreamEventIdentity()
    {
        var tracker = CreateTracker();
        var execution = tracker.Begin(
            "rule-7",
            "Raid",
            "eventsub-message-9",
            "queue-slot-3",
            "Raid",
            DateTimeOffset.UnixEpoch,
            CancellationToken.None);

        Assert.AreEqual("eventsub-message-9", execution.Context.EventMessageId);
        Assert.AreEqual("queue-slot-3", execution.Context.QueueSlotId);
        Assert.AreEqual("rule-7", execution.Context.RuleId);
    }

    [TestMethod]
    public void LifecycleMovesFromStartingToRunning()
    {
        var execution = Begin(CreateTracker());
        Assert.AreEqual(AlertExecutionState.Starting, execution.Trace.State);

        execution.MarkRunning();

        Assert.AreEqual(AlertExecutionState.Running, execution.Trace.State);
    }

    private static AlertExecutionTracker CreateTracker(
        Func<string>? idFactory = null,
        int maxExecutions = 50,
        int maxActions = 32) =>
        new(new ManualAlertTimeProvider(), idFactory, maxExecutions, maxActions);

    private static AlertExecutionScope Begin(
        AlertExecutionTracker tracker,
        CancellationToken cancellationToken = default) =>
        tracker.Begin("rule-1", "Follow", "message-1", "slot-1", "Follow", DateTimeOffset.UnixEpoch, cancellationToken);
}

[TestClass]
public sealed class AlertExternalActionTests
{
    [TestMethod]
    public async Task PendingTwitchChatObservesCancellation()
    {
        var handler = new BlockingHttpHandler();
        using var http = new HttpClient(handler);
        using var service = new TwitchChatService(UiTextService.CreateDefault(), http);
        using var cancellation = new CancellationTokenSource();
        var task = service.SendMessageAsync(CreateChatConfig(), "hello", cancellation.Token);
        await handler.Started;

        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => task);
    }

    [TestMethod]
    public async Task TwitchChatFailureIsRecordedWithoutRemoteBody()
    {
        const string secretBody = "server echoed access-token-secret";
        using var http = new HttpClient(new FailureHttpHandler(secretBody));
        using var service = new TwitchChatService(UiTextService.CreateDefault(), http);
        var execution = BeginExecution();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => execution.RunActionAsync(
            "TwitchChat",
            token => service.SendMessageAsync(CreateChatConfig(), "hello", token),
            "Twitch chat request failed"));
        execution.Finish();

        Assert.AreEqual(AlertExecutionState.Failed, execution.Trace.State);
        Assert.IsFalse(execution.Trace.Actions.Single().Reason.Contains(secretBody, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task PendingAlexaActionObservesCancellation()
    {
        var handler = new BlockingHttpHandler();
        using var http = new HttpClient(handler);
        var service = new AlexaRelayService(UiTextService.CreateDefault(), new ManualAlertTimeProvider(), http);
        using var cancellation = new CancellationTokenSource();
        var task = service.SendRuleEventAsync(CreateAlexaConfig(), CreateAlexaRule(), new TwitchEvent(), cancellation.Token);
        await handler.Started;

        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => task);
    }

    [TestMethod]
    public async Task AlexaFailureIsRecordedWithoutRemoteBody()
    {
        const string secretBody = "server echoed alexa-token-secret";
        using var http = new HttpClient(new FailureHttpHandler(secretBody));
        var service = new AlexaRelayService(UiTextService.CreateDefault(), new ManualAlertTimeProvider(), http);
        var execution = BeginExecution();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => execution.RunActionAsync(
            "Alexa",
            token => service.SendRuleEventAsync(CreateAlexaConfig(), CreateAlexaRule(), new TwitchEvent(), token),
            "Alexa request failed"));
        execution.Finish();

        Assert.AreEqual(AlertExecutionState.Failed, execution.Trace.State);
        Assert.IsFalse(execution.Trace.Actions.Single().Reason.Contains(secretBody, StringComparison.Ordinal));
    }

    private static AlertExecutionScope BeginExecution() => new AlertExecutionTracker(new ManualAlertTimeProvider())
        .Begin("rule", "Rule", "message", "slot", "Test", DateTimeOffset.UnixEpoch, CancellationToken.None);

    private static AppConfig CreateChatConfig() => new()
    {
        TwitchClientId = "client-id",
        Token = new TwitchTokenInfo { AccessToken = "access-token-secret" },
        Channel = new TwitchChannelInfo { UserId = "user-1", Login = "channel" }
    };

    private static AppConfig CreateAlexaConfig() => new()
    {
        Alexa = new AlexaIntegrationConfig
        {
            Enabled = true,
            RelayUrl = "https://relay.example/hook",
            AuthToken = "alexa-token-secret"
        }
    };

    private static EventRule CreateAlexaRule() => new() { Name = "Rule", SendAlexaEvent = true };

    private sealed class BlockingHttpHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Started => _started.Task;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Unreachable blocking handler continuation.");
        }
    }

    private sealed class FailureHttpHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(body)
            });
    }
}

internal sealed class ManualAlertTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow = new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan duration) => _utcNow += duration;
}
