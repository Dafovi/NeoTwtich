using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoTwitch.Installer;
using NeoTwitch.Services;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.Models;
using NeoTwitch.Shared;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Text;

[TestClass]
public sealed class InstallerProcessWaiterTests
{
    [TestMethod] public async Task AlreadyStoppedReturns() =>
        await Create(false).WaitForExitAsync(Progress(), CancellationToken.None, TimeSpan.FromMilliseconds(2), TimeSpan.FromMilliseconds(1));

    [TestMethod] public async Task StopsDuringWaitReturns()
    {
        var probe = new SequenceProcessProbe(true, false);
        await new InstallerProcessWaiter(probe, (_, _) => Task.CompletedTask)
            .WaitForExitAsync(Progress(), CancellationToken.None, TimeSpan.FromMilliseconds(2), TimeSpan.FromMilliseconds(1));
        Assert.AreEqual(2, probe.Calls);
    }

    [TestMethod] public async Task StillRunningFailsClosed() =>
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Create(true).WaitForExitAsync(
            Progress(), CancellationToken.None, TimeSpan.FromMilliseconds(2), TimeSpan.FromMilliseconds(1)));

    [TestMethod] public async Task CancellationStopsWait()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => Create(true).WaitForExitAsync(
            Progress(), cts.Token, TimeSpan.FromMilliseconds(2), TimeSpan.FromMilliseconds(1)));
    }

    [TestMethod] public async Task EnumerationFailureFailsClosed() =>
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => new InstallerProcessWaiter(new ThrowingProcessProbe())
            .WaitForExitAsync(Progress(), CancellationToken.None, TimeSpan.FromMilliseconds(2), TimeSpan.FromMilliseconds(1)));

    private static InstallerProcessWaiter Create(bool running) =>
        new(new SequenceProcessProbe(running), (_, _) => Task.CompletedTask);
    private static IProgress<InstallProgress> Progress() => new Progress<InstallProgress>();
}

[TestClass]
public sealed class EventSubMessageLimitTests
{
    [TestMethod] public void AcceptsExactLimit()
    {
        var accumulator = new EventSubMessageAccumulator();
        accumulator.Append(new byte[EventSubMessageAccumulator.MaximumMessageBytes]);
        Assert.AreEqual(EventSubMessageAccumulator.MaximumMessageBytes, accumulator.Length);
    }

    [TestMethod] public void RejectsLimitPlusOne() => Assert.ThrowsExactly<EventSubMessageTooLargeException>(() =>
    {
        var accumulator = new EventSubMessageAccumulator();
        accumulator.Append(new byte[EventSubMessageAccumulator.MaximumMessageBytes]);
        accumulator.Append([0]);
    });

    [TestMethod] public void EnforcesLimitAcrossFragments() => Assert.ThrowsExactly<EventSubMessageTooLargeException>(() =>
    {
        var accumulator = new EventSubMessageAccumulator();
        accumulator.Append(new byte[200_000]);
        accumulator.Append(new byte[70_000]);
    });
}

[TestClass]
public sealed class EventSubMigrationTests
{
    [TestMethod] public async Task OldSocketLivesUntilNewWelcome()
    {
        var oldSocket = FakeEventSubSocket.Pending();
        var newSocket = FakeEventSubSocket.Welcome("new-session", () => Assert.IsFalse(oldSocket.Disposed));
        await using var client = CreateClient(newSocket);
        var result = await client.MigrateSessionAsync(oldSocket, "old-session", Freshness(), "wss://example.test/reconnect", CancellationToken.None);
        Assert.IsTrue(result.Succeeded); Assert.IsFalse(oldSocket.Disposed);
        await result.OldSocket!.DisposeAsync(); await result.Socket!.DisposeAsync();
    }

    [TestMethod] public async Task PromotionExposesNewSessionMetadata()
    {
        var oldSocket = FakeEventSubSocket.Pending(); var newSocket = FakeEventSubSocket.Welcome("promoted");
        await using var client = CreateClient(newSocket);
        var result = await client.MigrateSessionAsync(oldSocket, "old", Freshness(), "wss://example.test/reconnect", CancellationToken.None);
        Assert.AreEqual("promoted", result.SessionId); Assert.IsNotNull(result.Freshness);
        await result.OldSocket!.DisposeAsync(); await result.Socket!.DisposeAsync();
    }

    [TestMethod] public async Task FailedMigrationKeepsOldSocketUsable()
    {
        using var cts = new CancellationTokenSource(); var oldSocket = FakeEventSubSocket.Pending();
        await using var client = CreateClient(FakeEventSubSocket.ConnectFailure());
        var result = await client.MigrateSessionAsync(oldSocket, "old", Freshness(), "wss://example.test/reconnect", cts.Token);
        Assert.IsFalse(result.Succeeded); Assert.IsFalse(oldSocket.Disposed); Assert.AreEqual(WebSocketState.Open, oldSocket.State);
        cts.Cancel(); try { await result.OldReader!; } catch (OperationCanceledException) { }
        await oldSocket.DisposeAsync();
    }

    [TestMethod] public void OverlapUsesSharedMessageIdDeduplication()
    {
        var deduplicator = new EventSubNotificationDeduplicator(TimeProvider.System);
        Assert.IsTrue(deduplicator.TryAccept("overlap-message"));
        Assert.IsFalse(deduplicator.TryAccept("overlap-message"));
    }

    private static EventSubConnectionFreshness Freshness() =>
        new(TimeProvider.System, TimeSpan.FromSeconds(30));

    private static TwitchEventSubClient CreateClient(IEventSubWebSocket newSocket)
    {
        var text = UiTextService.CreateDefault();
        var auth = new TwitchAuthService(text, new ExternalLauncherService(), TimeProvider.System);
        return new TwitchEventSubClient(auth, () => new AppConfig(), () => { }, _ => { }, text,
            null, TimeProvider.System, null, () => newSocket);
    }
}

[TestClass]
public sealed class InstallSwapTransactionTests
{
    [TestMethod] public void SuccessfulSwapCanCommit()
    {
        using var scope = new HardeningTempDirectory();
        var target = scope.Child("app"); var stage = scope.Child(".app.staging");
        WriteInstallation(target, "old"); WriteInstallation(stage, "new");
        using var transaction = InstallSwapTransaction.Activate(stage, target);
        transaction.Commit();
        Assert.AreEqual("new", File.ReadAllText(Path.Combine(target, "version.txt")));
    }

    [TestMethod] public void DisposeRollsBackPreviousInstallation()
    {
        using var scope = new HardeningTempDirectory();
        var target = scope.Child("app"); var stage = scope.Child(".app.staging");
        WriteInstallation(target, "old"); WriteInstallation(stage, "new");
        using (InstallSwapTransaction.Activate(stage, target)) { }
        Assert.AreEqual("old", File.ReadAllText(Path.Combine(target, "version.txt")));
    }

    [TestMethod] public void InvalidStageLeavesOldUntouched()
    {
        using var scope = new HardeningTempDirectory();
        var target = scope.Child("app"); var stage = scope.Child(".app.staging");
        WriteInstallation(target, "old"); Directory.CreateDirectory(stage);
        Assert.ThrowsExactly<InvalidOperationException>(() => InstallSwapTransaction.Activate(stage, target));
        Assert.AreEqual("old", File.ReadAllText(Path.Combine(target, "version.txt")));
    }

    [TestMethod] public void NewInstallationRollsBackToAbsent()
    {
        using var scope = new HardeningTempDirectory();
        var target = scope.Child("app"); var stage = scope.Child(".app.staging"); WriteInstallation(stage, "new");
        using (InstallSwapTransaction.Activate(stage, target)) { }
        Assert.IsFalse(Directory.Exists(target));
    }

    [TestMethod] public void MarkerSurvivesCommittedSwap()
    {
        using var scope = new HardeningTempDirectory();
        var target = scope.Child("app"); var stage = scope.Child(".app.staging"); WriteInstallation(stage, "new");
        using var transaction = InstallSwapTransaction.Activate(stage, target); transaction.Commit();
        Assert.IsTrue(File.Exists(Path.Combine(target, NeoTwitchProduct.InstallMarkerFileName)));
    }

    [TestMethod] public void ChangedDestinationKindIsRejected()
    {
        using var scope = new HardeningTempDirectory();
        var target = scope.Child("app"); var stage = scope.Child(".app.staging");
        Directory.CreateDirectory(target); WriteInstallation(stage, "new");
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            InstallSwapTransaction.Activate(stage, target, InstallTargetKind.ExistingNeoTwitchInstallation));
        Assert.IsTrue(Directory.Exists(stage));
    }

    [TestMethod] public void RollbackPreservesConfigurationBytes()
    {
        using var scope = new HardeningTempDirectory();
        var target = scope.Child("app"); var stage = scope.Child(".app.staging");
        WriteInstallation(target, "old"); WriteInstallation(stage, "new");
        var settings = Path.Combine(target, "settings.json"); File.WriteAllText(settings, "opaque-config");
        using (InstallSwapTransaction.Activate(stage, target)) { }
        Assert.AreEqual("opaque-config", File.ReadAllText(settings));
    }

    [TestMethod] public void CrossDirectoryStagingIsRejected()
    {
        using var first = new HardeningTempDirectory(); using var second = new HardeningTempDirectory();
        var stage = first.Child("stage"); var target = second.Child("app"); WriteInstallation(stage, "new");
        Assert.ThrowsExactly<InvalidOperationException>(() => InstallSwapTransaction.Activate(stage, target));
    }

    private static void WriteInstallation(string path, string version)
    {
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, NeoTwitchProduct.AppExecutableName), "exe");
        File.WriteAllText(Path.Combine(path, NeoTwitchProduct.InstallMarkerFileName),
            $"{{\"productId\":\"{NeoTwitchProduct.ProductIdentifier}\",\"schemaVersion\":{NeoTwitchProduct.InstallMarkerSchemaVersion},\"version\":\"{version}\"}}");
        File.WriteAllText(Path.Combine(path, "version.txt"), version);
    }
}

[TestClass]
public sealed class InstallerConfigurationIsolationTests
{
    [TestMethod] public Task ValidSettingsRemainUnchanged() => VerifyPreservedAsync("{\"schemaVersion\":1,\"token\":\"protected\"}");
    [TestMethod] public Task CorruptSettingsRemainUnchanged() => VerifyPreservedAsync("{ definitely-not-json");
    [TestMethod] public Task FutureSchemaSettingsRemainUnchanged() => VerifyPreservedAsync("{\"schemaVersion\":999,\"future\":true}");

    [TestMethod] public async Task CorruptPrimaryAndValidBackupRemainUnchanged()
    {
        using var scope = new HardeningTempDirectory();
        var config = scope.Child("config"); Directory.CreateDirectory(config);
        var primary = Path.Combine(config, "settings.json"); var backup = primary + ".bak";
        File.WriteAllText(primary, "corrupt"); File.WriteAllText(backup, "{\"schemaVersion\":1}");
        await RunUpdateAsync(scope);
        Assert.AreEqual("corrupt", File.ReadAllText(primary));
        Assert.AreEqual("{\"schemaVersion\":1}", File.ReadAllText(backup));
    }

    [TestMethod] public async Task InstallationDoesNotCreateMinimalSettings()
    {
        using var scope = new HardeningTempDirectory();
        var settings = scope.Child("config/settings.json");
        await RunUpdateAsync(scope);
        Assert.IsFalse(File.Exists(settings));
    }

    [TestMethod] public async Task UpdateSucceedsWithoutTouchingSettings()
    {
        using var scope = new HardeningTempDirectory();
        var config = scope.Child("config"); Directory.CreateDirectory(config);
        var settings = Path.Combine(config, "settings.json"); File.WriteAllText(settings, "opaque-user-data");
        await RunUpdateAsync(scope);
        Assert.AreEqual("new", File.ReadAllText(Path.Combine(scope.Child("app"), "payload.txt")));
        Assert.AreEqual("opaque-user-data", File.ReadAllText(settings));
    }

    private static async Task VerifyPreservedAsync(string content)
    {
        using var scope = new HardeningTempDirectory();
        var config = scope.Child("config"); Directory.CreateDirectory(config);
        var settings = Path.Combine(config, "settings.json"); File.WriteAllText(settings, content);
        await RunUpdateAsync(scope);
        Assert.AreEqual(content, File.ReadAllText(settings));
    }

    private static async Task RunUpdateAsync(HardeningTempDirectory scope)
    {
        var target = scope.Child("app"); Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, NeoTwitchProduct.AppExecutableName), "old");
        File.WriteAllText(Path.Combine(target, NeoTwitchProduct.InstallMarkerFileName),
            "{\"productId\":\"com.dafovi.neotwitch\",\"schemaVersion\":1,\"version\":\"1.0.0\"}");
        var packageRoot = scope.Child("package"); Directory.CreateDirectory(packageRoot);
        File.WriteAllText(Path.Combine(packageRoot, NeoTwitchProduct.AppExecutableName), "new");
        File.WriteAllText(Path.Combine(packageRoot, "payload.txt"), "new");
        var zip = scope.Child("package.zip"); ZipFile.CreateFromDirectory(packageRoot, zip);

        var waiter = new InstallerProcessWaiter(new SequenceProcessProbe(false), (_, _) => Task.CompletedTask);
        var service = new InstallerService(new FixedReleaseClient(zip), TimeProvider.System, waiter, (_, _) => { }, _ => { });
        await service.InstallAsync(new InstallerOptions
        {
            InstallPath = target,
            IsUpdate = true,
            CreateDesktopShortcut = false,
            CreateStartMenuShortcut = false,
            LaunchAfterInstall = false
        }, new Progress<InstallProgress>(), CancellationToken.None);
    }
}

[TestClass]
public sealed class AudioPlayerOwnershipTests
{
    [TestMethod] public async Task PreparationTransfersOwnership()
    {
        using var file = new HardeningTempFile(); var player = new FakeAudioPlayer(FakeAudioBehavior.Open);
        await using var service = Create(player); var playback = await service.PrepareAsync(file.Path, 50, _ => { });
        Assert.IsNotNull(playback); Assert.AreEqual(1, service.TrackedPlayerCount); Assert.IsFalse(player.Closed);
    }

    [TestMethod] public async Task FailedPreparationCleansUp()
    {
        using var file = new HardeningTempFile(); var player = new FakeAudioPlayer(FakeAudioBehavior.Fail);
        await using var service = Create(player); Assert.IsNull(await service.PrepareAsync(file.Path, 50, _ => { }));
        AssertClean(service, player);
    }

    [TestMethod] public async Task PreparationTimeoutCleansUp()
    {
        using var file = new HardeningTempFile(); var player = new FakeAudioPlayer(FakeAudioBehavior.None);
        await using var service = Create(player); Assert.IsNull(await service.PrepareAsync(file.Path, 50, _ => { }));
        AssertClean(service, player);
    }

    [TestMethod] public async Task ProbeSuccessCleansUp()
    {
        using var file = new HardeningTempFile(); var player = new FakeAudioPlayer(FakeAudioBehavior.Open);
        await using var service = Create(player); Assert.IsNotNull(await service.ProbeDurationAsync(file.Path)); Assert.IsTrue(player.Closed);
    }

    [TestMethod] public async Task ProbeFailureCleansUp()
    {
        using var file = new HardeningTempFile(); var player = new FakeAudioPlayer(FakeAudioBehavior.Fail);
        await using var service = Create(player); Assert.IsNull(await service.ProbeDurationAsync(file.Path)); Assert.IsTrue(player.Closed);
    }

    [TestMethod] public async Task ProbeTimeoutCleansUp()
    {
        using var file = new HardeningTempFile(); var player = new FakeAudioPlayer(FakeAudioBehavior.None);
        await using var service = Create(player); Assert.IsNull(await service.ProbeDurationAsync(file.Path)); Assert.IsTrue(player.Closed);
    }

    [TestMethod] public async Task CancellationCleansUp()
    {
        using var file = new HardeningTempFile(); var player = new FakeAudioPlayer(FakeAudioBehavior.None);
        await using var service = Create(player); using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.ProbeDurationAsync(file.Path, cts.Token));
        Assert.IsTrue(player.Closed);
    }

    [TestMethod] public async Task PlaybackReleaseEndsOwnership()
    {
        using var file = new HardeningTempFile(); var player = new FakeAudioPlayer(FakeAudioBehavior.Open);
        await using var service = Create(player); var playback = await service.PrepareAsync(file.Path, 50, _ => { });
        player.End(); await playback!.Completion; AssertClean(service, player);
    }

    [TestMethod] public async Task ShutdownClosesTrackedPlayers()
    {
        using var file = new HardeningTempFile(); var player = new FakeAudioPlayer(FakeAudioBehavior.Open);
        var service = Create(player); _ = await service.PrepareAsync(file.Path, 50, _ => { }); await service.DisposeAsync();
        AssertClean(service, player);
    }

    private static AudioPlayerService Create(FakeAudioPlayer player) =>
        new(UiTextService.CreateDefault(), () => player, new ImmediateAudioDispatcher(), TimeSpan.FromMilliseconds(10));
    private static void AssertClean(AudioPlayerService service, FakeAudioPlayer player)
    { Assert.AreEqual(0, service.TrackedPlayerCount); Assert.IsTrue(player.Closed); }
}

internal sealed class SequenceProcessProbe(params bool[] states) : IInstallerProcessProbe
{
    private int _index;
    public int Calls { get; private set; }
    public bool IsNeoTwitchRunning() { Calls++; return states[Math.Min(_index++, states.Length - 1)]; }
}
internal sealed class ThrowingProcessProbe : IInstallerProcessProbe { public bool IsNeoTwitchRunning() => throw new InvalidOperationException("probe"); }
internal sealed class FixedReleaseClient(string packagePath) : IReleaseClient
{
    public Task<VerifiedReleaseAsset> DownloadLatestVerifiedAsync(string targetDirectory, IProgress<InstallProgress> progress,
        CancellationToken cancellationToken) => Task.FromResult(new VerifiedReleaseAsset("2.0.0", "package.zip", packagePath, ""));
}
internal sealed class ImmediateAudioDispatcher : IAudioDispatcher
{
    public void Post(Action action) => action();
    public Task InvokeAsync(Action action) { action(); return Task.CompletedTask; }
}
internal sealed class FakeEventSubSocket : IEventSubWebSocket
{
    private readonly byte[]? _message;
    private readonly Exception? _connectException;
    private readonly Action? _beforeReceive;
    private FakeEventSubSocket(byte[]? message, Exception? connectException, Action? beforeReceive)
    { _message = message; _connectException = connectException; _beforeReceive = beforeReceive; }
    public WebSocketState State { get; private set; } = WebSocketState.Open;
    public bool Disposed { get; private set; }
    public static FakeEventSubSocket Pending() => new(null, null, null);
    public static FakeEventSubSocket ConnectFailure() => new(null, new WebSocketException("connect failed"), null);
    public static FakeEventSubSocket Welcome(string sessionId, Action? beforeReceive = null) => new(Encoding.UTF8.GetBytes(
        $"{{\"metadata\":{{\"message_type\":\"session_welcome\"}},\"payload\":{{\"session\":{{\"id\":\"{sessionId}\",\"keepalive_timeout_seconds\":30}}}}}}"), null, beforeReceive);
    public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) =>
        _connectException is null ? Task.CompletedTask : Task.FromException(_connectException);
    public async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        _beforeReceive?.Invoke();
        if (_message is null) { await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); throw new InvalidOperationException(); }
        _message.CopyTo(buffer.Array!, buffer.Offset);
        return new WebSocketReceiveResult(_message.Length, WebSocketMessageType.Text, true);
    }
    public Task CloseAsync(WebSocketCloseStatus status, string description, CancellationToken cancellationToken)
    { State = WebSocketState.Closed; return Task.CompletedTask; }
    public void Abort() => State = WebSocketState.Aborted;
    public ValueTask DisposeAsync() { Disposed = true; State = WebSocketState.Closed; return ValueTask.CompletedTask; }
}
internal enum FakeAudioBehavior { None, Open, Fail }
internal sealed class FakeAudioPlayer(FakeAudioBehavior behavior) : IAudioMediaPlayer
{
    public event EventHandler? Opened; public event EventHandler? Ended; public event EventHandler<AudioMediaFailedEventArgs>? Failed;
    public TimeSpan? Duration => TimeSpan.FromSeconds(1); public double Volume { private get; set; }
    public bool Closed { get; private set; }
    public void Open(Uri uri) { if (behavior == FakeAudioBehavior.Open) Opened?.Invoke(this, EventArgs.Empty); else if (behavior == FakeAudioBehavior.Fail) Failed?.Invoke(this, new(new IOException("fail"))); }
    public void Play() { } public void Stop() { } public void Close() => Closed = true;
    public void End() => Ended?.Invoke(this, EventArgs.Empty);
}
internal sealed class HardeningTempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"NeoTwitchHardening-{Guid.NewGuid():N}");
    public HardeningTempDirectory() => Directory.CreateDirectory(Path);
    public string Child(string name) => System.IO.Path.Combine(Path, name);
    public void Dispose() { if (Directory.Exists(Path)) Directory.Delete(Path, true); }
}
internal sealed class HardeningTempFile : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"NeoTwitchAudio-{Guid.NewGuid():N}.wav");
    public HardeningTempFile() => File.WriteAllBytes(Path, [0]);
    public void Dispose() { if (File.Exists(Path)) File.Delete(Path); }
}
