using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoTwitch.Models;
using NeoTwitch.Services.Alerts;

namespace NeoTwitch.Tests;

[TestClass]
public sealed class AlertExecutionCoordinatorTests
{
    [TestMethod]
    public async Task ExecutesSuccessfullyWithoutMainWindow()
    {
        var (coordinator, _) = CreateCoordinator();
        var capabilities = new FakeCapabilities();

        var result = await coordinator.ExecuteAsync(CreateRequest(), capabilities);

        Assert.IsTrue(result.IsCompleted);
        Assert.AreEqual(1, capabilities.EffectsCalls);
        Assert.AreEqual(1, capabilities.CleanupCalls);
    }

    [TestMethod]
    public async Task ExecutesCoreBeforeCleanup()
    {
        var (coordinator, _) = CreateCoordinator();
        var capabilities = new FakeCapabilities();

        await coordinator.ExecuteAsync(CreateRequest(), capabilities);

        CollectionAssert.AreEqual(new[] { "effects", "cleanup" }, capabilities.Events);
    }

    [TestMethod]
    public async Task StartsOptionalActionConcurrentlyWithEffects()
    {
        var (coordinator, _) = CreateCoordinator();
        var chatRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var effectsStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var capabilities = new FakeCapabilities
        {
            Chat = _ => chatRelease.Task,
            Effects = (_, _, _) => { effectsStarted.TrySetResult(); return Task.CompletedTask; }
        };

        var execution = coordinator.ExecuteAsync(CreateRequest(chat: true), capabilities);
        await effectsStarted.Task;
        Assert.IsFalse(execution.IsCompleted);
        chatRelease.TrySetResult();

        Assert.IsTrue((await execution).IsCompleted);
    }

    [TestMethod]
    public async Task CancelCurrentReturnsCancelledResult()
    {
        var (coordinator, _) = CreateCoordinator();
        var effectsStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var capabilities = new FakeCapabilities
        {
            Effects = async (_, _, token) =>
            {
                effectsStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
        };

        var execution = coordinator.ExecuteAsync(CreateRequest(), capabilities);
        await effectsStarted.Task;
        Assert.IsTrue(coordinator.CancelCurrent());
        var result = await execution;

        Assert.IsTrue(result.IsCancelled);
        Assert.IsTrue(capabilities.CleanupWasCancelled);
        Assert.IsFalse(coordinator.IsRunning);
    }

    [TestMethod]
    public async Task OptionalActionFailureDoesNotPreventEffects()
    {
        var (coordinator, _) = CreateCoordinator();
        var capabilities = new FakeCapabilities
        {
            Chat = _ => throw new InvalidOperationException("chat unavailable")
        };

        var result = await coordinator.ExecuteAsync(CreateRequest(chat: true), capabilities);

        Assert.IsTrue(result.IsFailed);
        Assert.AreEqual(1, capabilities.EffectsCalls);
        Assert.AreEqual(AlertActionState.Failed, result.Trace.Actions.Single(action => action.ActionType == "TwitchChat").State);
    }

    [TestMethod]
    public async Task CoreFailureProducesFailedResult()
    {
        var (coordinator, _) = CreateCoordinator();
        var capabilities = new FakeCapabilities
        {
            Effects = (_, _, _) => throw new InvalidOperationException("core failure")
        };

        var result = await coordinator.ExecuteAsync(CreateRequest(), capabilities);

        Assert.IsTrue(result.IsFailed);
        Assert.AreEqual(AlertActionState.Failed, result.Trace.Actions.Single(action => action.ActionType == "Effects").State);
    }

    [TestMethod]
    public async Task CleanupRunsAfterFailure()
    {
        var (coordinator, _) = CreateCoordinator();
        var capabilities = new FakeCapabilities
        {
            Effects = (_, _, _) => throw new InvalidOperationException("core failure")
        };

        await coordinator.ExecuteAsync(CreateRequest(), capabilities);

        Assert.AreEqual(1, capabilities.CleanupCalls);
        CollectionAssert.AreEqual(new[] { "effects", "cleanup" }, capabilities.Events);
    }

    [TestMethod]
    public async Task CleanupFailureProducesFailedResult()
    {
        var (coordinator, _) = CreateCoordinator();
        var capabilities = new FakeCapabilities
        {
            Cleanup = (_, _) => throw new InvalidOperationException("cleanup failure")
        };

        var result = await coordinator.ExecuteAsync(CreateRequest(), capabilities);

        Assert.IsTrue(result.IsFailed);
    }

    [TestMethod]
    public async Task ResultExposesExactlyOneTerminalState()
    {
        var (coordinator, _) = CreateCoordinator();

        var result = await coordinator.ExecuteAsync(CreateRequest(), new FakeCapabilities());

        Assert.IsTrue(result.IsCompleted);
        Assert.IsFalse(result.IsCancelled);
        Assert.IsFalse(result.IsFailed);
    }

    [TestMethod]
    public async Task EveryActionCorrelatesToExecutionId()
    {
        var (coordinator, _) = CreateCoordinator();

        var result = await coordinator.ExecuteAsync(CreateRequest(chat: true, alexa: true), new FakeCapabilities());

        Assert.IsTrue(result.Trace.Actions.Count >= 3);
        Assert.IsTrue(result.Trace.Actions.All(action => action.ExecutionId == result.ExecutionId));
    }

    [TestMethod]
    public async Task NoFurtherCapabilityStartsAfterCancellation()
    {
        var (coordinator, _) = CreateCoordinator();
        var capabilities = new FakeCapabilities();
        capabilities.Chat = _ =>
        {
            coordinator.CancelCurrent("cancelled by first action");
            return Task.CompletedTask;
        };

        var result = await coordinator.ExecuteAsync(CreateRequest(chat: true, alexa: true), capabilities);

        Assert.IsTrue(result.IsCancelled);
        Assert.AreEqual(1, capabilities.ChatCalls);
        Assert.AreEqual(0, capabilities.AlexaCalls);
        Assert.AreEqual(0, capabilities.EffectsCalls);
    }

    [TestMethod]
    public void SnapshotDoesNotChangeWhenEditorRuleChanges()
    {
        var rule = new EventRule { Name = "Original", ChatMessageTemplate = "hello", PrimaryColor = "#112233" };
        var snapshot = AlertExecutionSnapshotFactory.Create(rule);

        rule.Name = "Edited";
        rule.ChatMessageTemplate = "changed";
        rule.PrimaryColor = "#FFFFFF";

        Assert.AreEqual("Original", snapshot.RuleName);
        Assert.AreEqual("hello", snapshot.Chat.MessageTemplate);
        Assert.AreEqual("#112233", snapshot.Lights.PrimaryColor);
    }

    [TestMethod]
    public void ConcurrentSnapshotsDoNotShareActionConfiguration()
    {
        var rule = new EventRule { ChatMessageTemplate = "first" };
        var first = AlertExecutionSnapshotFactory.Create(rule);
        rule.ChatMessageTemplate = "second";
        var second = AlertExecutionSnapshotFactory.Create(rule);

        Assert.AreNotSame(first.Chat, second.Chat);
        Assert.AreEqual("first", first.Chat.MessageTemplate);
        Assert.AreEqual("second", second.Chat.MessageTemplate);
    }

    [TestMethod]
    public void SnapshotExcludesTransientEditorAvailability()
    {
        var names = typeof(AlertExecutionRuleSnapshot).GetProperties().Select(property => property.Name).ToArray();

        CollectionAssert.DoesNotContain(names, "LightsActionAvailable");
        CollectionAssert.DoesNotContain(names, "AlexaActionAvailable");
        CollectionAssert.DoesNotContain(names, "ObsActionAvailable");
    }

    [TestMethod]
    public async Task DispatcherDependentWorkUsesExplicitCapabilityBoundary()
    {
        var (coordinator, _) = CreateCoordinator();
        var capabilities = new FakeCapabilities { RequiresUiBoundary = true };

        await coordinator.ExecuteAsync(CreateRequest(), capabilities);

        Assert.IsTrue(capabilities.UiBoundaryInvoked);
    }

    [TestMethod]
    public async Task QueueSlotIsCorrelatedAndReleasedByCoordinator()
    {
        var time = new ManualAlertTimeProvider();
        var queue = new AlertQueueService(time, () => "slot-1");
        var coordinator = new AlertExecutionCoordinator(new AlertExecutionTracker(time, () => "execution-1"), queue);
        var persistedRule = new EventRule { Id = "rule-1", Name = "Rule" };
        var twitchEvent = new TwitchEvent { Kind = TwitchEventKind.Follow };
        var slot = queue.TryReserve(persistedRule, twitchEvent, false, new AlertQueueOptions(1, 0, 1, 0), out _);

        var result = await coordinator.ExecuteAsync(CreateRequest(slot: slot), new FakeCapabilities());

        Assert.AreEqual("slot-1", result.Trace.Context.QueueSlotId);
        Assert.IsFalse(coordinator.IsRunning);
    }

    private static (AlertExecutionCoordinator Coordinator, AlertExecutionTracker Tracker) CreateCoordinator()
    {
        var time = new ManualAlertTimeProvider();
        var tracker = new AlertExecutionTracker(time, () => "execution-1");
        return (new AlertExecutionCoordinator(tracker, new AlertQueueService(time)), tracker);
    }

    private static AlertExecutionRequest CreateRequest(bool chat = false, bool alexa = false, QueuedAlertSlot? slot = null)
    {
        var rule = new EventRule
        {
            Id = "rule-1",
            Name = "Rule",
            EventKind = TwitchEventKind.Follow,
            SendChatMessage = chat,
            SendAlexaEvent = alexa
        };
        return new AlertExecutionRequest(
            AlertExecutionSnapshotFactory.Create(rule),
            AlertExecutionSnapshotFactory.Create(new TwitchEvent { Kind = TwitchEventKind.Follow, EventSubMessageId = "message-1" }),
            slot,
            chat,
            alexa);
    }

    private sealed class FakeCapabilities : IAlertExecutionCapabilities
    {
        public List<string> Events { get; } = [];
        public Func<CancellationToken, Task> Chat { get; set; } = _ => Task.CompletedTask;
        public Func<CancellationToken, Task> Alexa { get; set; } = _ => Task.CompletedTask;
        public Func<AlertExecutionScope, IAlertExecutionCapabilityState, CancellationToken, Task> Effects { get; set; } = (_, _, _) => Task.CompletedTask;
        public Func<IAlertExecutionCapabilityState, bool, Task> Cleanup { get; set; } = (_, _) => Task.CompletedTask;
        public int ChatCalls { get; private set; }
        public int AlexaCalls { get; private set; }
        public int EffectsCalls { get; private set; }
        public int CleanupCalls { get; private set; }
        public bool CleanupWasCancelled { get; private set; }
        public bool RequiresUiBoundary { get; set; }
        public bool UiBoundaryInvoked { get; private set; }

        public IAlertExecutionCapabilityState CreateState() => new FakeState();

        public Task ExecuteChatAsync(AlertExecutionRequest request, CancellationToken cancellationToken)
        {
            ChatCalls++;
            return Chat(cancellationToken);
        }

        public Task ExecuteAlexaAsync(AlertExecutionRequest request, CancellationToken cancellationToken)
        {
            AlexaCalls++;
            return Alexa(cancellationToken);
        }

        public Task ExecuteEffectsAsync(AlertExecutionRequest request, AlertExecutionScope execution, IAlertExecutionCapabilityState state, CancellationToken cancellationToken)
        {
            EffectsCalls++;
            Events.Add("effects");
            UiBoundaryInvoked = RequiresUiBoundary;
            return Effects(execution, state, cancellationToken);
        }

        public Task CleanupAsync(AlertExecutionRequest request, IAlertExecutionCapabilityState state, bool wasCancelled)
        {
            CleanupCalls++;
            CleanupWasCancelled = wasCancelled;
            Events.Add("cleanup");
            return Cleanup(state, wasCancelled);
        }

        private sealed class FakeState : IAlertExecutionCapabilityState;
    }
}
