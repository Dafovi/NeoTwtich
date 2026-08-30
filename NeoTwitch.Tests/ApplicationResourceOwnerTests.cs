using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoTwitch.Services;

namespace NeoTwitch.Tests;

[TestClass]
public sealed class ApplicationResourceOwnerTests
{
    [TestMethod]
    public async Task DisposesEveryRegisteredResource()
    {
        var owner = new ApplicationResourceOwner();
        var first = new FakeDisposable();
        var second = new FakeAsyncDisposable();
        owner.Register("first", 10, first);
        owner.Register("second", 20, second);

        await owner.DisposeAsync();

        Assert.AreEqual(1, first.DisposeCalls);
        Assert.AreEqual(1, second.DisposeCalls);
    }

    [TestMethod]
    public async Task RepeatedShutdownDisposesResourcesOnce()
    {
        var owner = new ApplicationResourceOwner();
        var resource = new FakeAsyncDisposable();
        owner.Register("resource", 10, resource);

        await owner.DisposeAsync();
        await owner.DisposeAsync();

        Assert.AreEqual(1, resource.DisposeCalls);
    }

    [TestMethod]
    public async Task HonorsDependencyShutdownOrder()
    {
        var events = new List<string>();
        var owner = new ApplicationResourceOwner();
        owner.Register("persistence", ApplicationShutdownOrder.Persistence, () => Record(events, "persistence"));
        owner.Register("ingress", ApplicationShutdownOrder.EventIngress, () => Record(events, "ingress"));
        owner.Register("connections", ApplicationShutdownOrder.Connections, () => Record(events, "connections"));

        await owner.DisposeAsync();

        CollectionAssert.AreEqual(new[] { "ingress", "connections", "persistence" }, events);
    }

    [TestMethod]
    public async Task AwaitsAsynchronousResourceShutdown()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var owner = new ApplicationResourceOwner();
        owner.Register("async", 10, () => new ValueTask(release.Task));

        var shutdown = owner.DisposeAsync().AsTask();
        Assert.IsFalse(shutdown.IsCompleted);
        release.TrySetResult();
        await shutdown;

        Assert.IsTrue(shutdown.IsCompletedSuccessfully);
    }

    [TestMethod]
    public async Task FailureDoesNotPreventRemainingDisposal()
    {
        var owner = new ApplicationResourceOwner();
        var remaining = new FakeDisposable();
        owner.Register("failure", 10, () => ValueTask.FromException(new IOException("failed")));
        owner.Register("remaining", 20, remaining);

        await owner.DisposeAsync();

        Assert.AreEqual(1, remaining.DisposeCalls);
        Assert.AreEqual("failure", owner.Failures.Single().ResourceName);
    }

    [TestMethod]
    public async Task PartialInitializationFailureDisposesCreatedResources()
    {
        var owner = new ApplicationResourceOwner();
        var created = new FakeDisposable();

        try
        {
            owner.Register("created", 10, created);
            throw new InvalidOperationException("startup failed");
        }
        catch (InvalidOperationException)
        {
            await owner.DisposeAsync();
        }

        Assert.AreEqual(1, created.DisposeCalls);
    }

    private static ValueTask Record(List<string> events, string value)
    {
        events.Add(value);
        return ValueTask.CompletedTask;
    }

    private sealed class FakeDisposable : IDisposable
    {
        public int DisposeCalls { get; private set; }
        public void Dispose() => DisposeCalls++;
    }

    private sealed class FakeAsyncDisposable : IAsyncDisposable
    {
        public int DisposeCalls { get; private set; }
        public ValueTask DisposeAsync()
        {
            DisposeCalls++;
            return ValueTask.CompletedTask;
        }
    }
}
