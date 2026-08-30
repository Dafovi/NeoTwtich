using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeoTwitch.Services;

namespace NeoTwitch.Tests;

[TestClass]
public sealed class CrashLogRetentionTests
{
    private string _directory = null!;
    private string _logPath = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "NeoTwitchCrashLogTests", Guid.NewGuid().ToString("N"));
        _logPath = Path.Combine(_directory, "crash.log");
    }

    [TestCleanup]
    public void Cleanup()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "NeoTwitchCrashLogTests"));
        var target = Path.GetFullPath(_directory);
        if (target.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && Directory.Exists(target))
        {
            Directory.Delete(target, recursive: true);
        }
    }

    [TestMethod]
    public void WritesNormalCrashEntry()
    {
        var writer = new CrashLogWriter(1024, 2);

        Assert.IsTrue(writer.TryWrite(_logPath, "normal crash"));

        StringAssert.Contains(File.ReadAllText(_logPath), "normal crash");
    }

    [TestMethod]
    public void RotatesWhenNextEntryWouldExceedLimit()
    {
        var writer = new CrashLogWriter(12, 2);
        writer.TryWrite(_logPath, "first-entry");

        writer.TryWrite(_logPath, "second-entry");

        Assert.IsTrue(File.Exists(Path.Combine(_directory, "crash.1.log")));
        StringAssert.Contains(File.ReadAllText(_logPath), "second-entry");
    }

    [TestMethod]
    public void RemovesOldestArchiveFirst()
    {
        var writer = new CrashLogWriter(12, 2);
        writer.TryWrite(_logPath, "oldest-entry");
        writer.TryWrite(_logPath, "middle-entry");
        writer.TryWrite(_logPath, "newest-entry");

        writer.TryWrite(_logPath, "active-entry");

        var retainedText = string.Join("|", Directory.GetFiles(_directory, "crash*.log").Select(File.ReadAllText));
        Assert.IsFalse(retainedText.Contains("oldest-entry", StringComparison.Ordinal));
        StringAssert.Contains(File.ReadAllText(Path.Combine(_directory, "crash.2.log")), "middle-entry");
    }

    [TestMethod]
    public void MaintainsConfiguredArchiveBound()
    {
        var writer = new CrashLogWriter(8, 2);
        for (var index = 0; index < 8; index++)
        {
            writer.TryWrite(_logPath, $"entry-{index}");
        }

        Assert.AreEqual(2, Directory.GetFiles(_directory, "crash.*.log").Length);
    }

    [TestMethod]
    public void RotationFailureDoesNotEscapeOrLoseActiveWrite()
    {
        var writer = new CrashLogWriter(8, 2, () => throw new IOException("rotation denied"));
        writer.TryWrite(_logPath, "first");

        var written = writer.TryWrite(_logPath, "second");

        Assert.IsTrue(written);
        var text = File.ReadAllText(_logPath);
        StringAssert.Contains(text, "first");
        StringAssert.Contains(text, "second");
    }

    [TestMethod]
    public void RotationNeverDeletesUnrelatedFiles()
    {
        Directory.CreateDirectory(_directory);
        var unrelated = Path.Combine(_directory, "notes.txt");
        File.WriteAllText(unrelated, "keep me");
        var writer = new CrashLogWriter(8, 1);

        writer.TryWrite(_logPath, "first");
        writer.TryWrite(_logPath, "second");
        writer.TryWrite(_logPath, "third");

        Assert.AreEqual("keep me", File.ReadAllText(unrelated));
    }
}
