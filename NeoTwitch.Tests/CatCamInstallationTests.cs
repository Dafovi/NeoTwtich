using System.Net;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoTwitch.Services.Integrations;

namespace NeoTwitch.Tests;

[TestClass]
public sealed class CatCamInstallationTests
{
    private string _root = null!;
    [TestInitialize] public void Initialize() => _root = Path.Combine(Path.GetTempPath(), "NeoTwitch-CatCam-tests", Guid.NewGuid().ToString("N"));
    [TestCleanup] public void Cleanup() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }

    [TestMethod]
    public void DeleteRejectsParentAndSiblingPaths()
    {
        var installation = new CatCamInstallation(_root);
        Assert.ThrowsExactly<InvalidOperationException>(() => installation.DeleteOwnedDirectory(_root));
        Assert.ThrowsExactly<InvalidOperationException>(() => installation.DeleteOwnedDirectory(_root + "-other"));
        Assert.ThrowsExactly<InvalidOperationException>(() => installation.DeleteOwnedDirectory(Path.Combine(_root, "..", "outside")));
    }

    [TestMethod]
    public async Task CancelledRepairPreservesReadyInstallation()
    {
        var installation = new CatCamInstallation(_root);
        Directory.CreateDirectory(Path.GetDirectoryName(installation.Python)!);
        Directory.CreateDirectory(installation.Project);
        File.WriteAllText(installation.Python, "runtime");
        File.WriteAllText(Path.Combine(installation.Project, "gesture_meme.py"), "upstream");
        File.WriteAllText(Path.Combine(installation.Current, "ready"), CatCamInstallation.Revision);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => installation.InstallAsync(new Progress<string>(), cancelled.Token));
        Assert.IsTrue(installation.IsInstalled);
        Assert.AreEqual("upstream", File.ReadAllText(Path.Combine(installation.Project, "gesture_meme.py")));
    }

    [TestMethod]
    public async Task LockedInstallationDoesNotDeleteAnotherInstallersStaging()
    {
        var installation = new CatCamInstallation(_root);
        Directory.CreateDirectory(Path.Combine(_root, "staging"));
        var sentinel = Path.Combine(_root, "staging", "download");
        File.WriteAllText(sentinel, "in progress");
        using var lease = new FileStream(Path.Combine(_root, "install.lock"), FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        await Assert.ThrowsExactlyAsync<IOException>(() => installation.InstallAsync(new Progress<string>(), CancellationToken.None));
        Assert.IsTrue(File.Exists(sentinel));
    }

    [TestMethod]
    public async Task DownloadRejectsTamperedContent()
    {
        Directory.CreateDirectory(_root);
        using var http = new HttpClient(new DownloadHandler());
        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => CatCamInstallation.DownloadAsync(http,
            "https://example.test/package", Path.Combine(_root, "package"), new string('0', 64), new Progress<string>(), "test", CancellationToken.None));
    }

    [TestMethod]
    public async Task DownloadAcceptsExactHash()
    {
        Directory.CreateDirectory(_root);
        using var http = new HttpClient(new DownloadHandler());
        var path = Path.Combine(_root, "package");
        await CatCamInstallation.DownloadAsync(http, "https://example.test/package", path,
            Convert.ToHexString(SHA256.HashData(new byte[] { 1, 2, 3 })), new Progress<string>(), "test", CancellationToken.None);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, File.ReadAllBytes(path));
    }

    [TestMethod]
    public async Task UninstallRemovesOnlyManagedFolders()
    {
        var installation = new CatCamInstallation(_root);
        Directory.CreateDirectory(installation.Current);
        File.WriteAllText(Path.Combine(_root, "user-note.txt"), "keep");
        await installation.UninstallAsync(CancellationToken.None);
        Assert.IsFalse(Directory.Exists(installation.Current));
        Assert.IsTrue(File.Exists(Path.Combine(_root, "user-note.txt")));
    }

    private sealed class DownloadHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });
    }
}
