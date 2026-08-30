using System.IO;
using System.Text;

namespace NeoTwitch.Services;

public static class CrashReporter
{
    public const long MaximumActiveLogBytes = 1024 * 1024;
    public const int RetainedArchiveCount = 4;
    private static readonly CrashLogWriter Writer = new(MaximumActiveLogBytes, RetainedArchiveCount);

    public static string PreferredLogPath => ApplicationPaths.CrashLogPath;

    public static string Log(Exception exception, string context)
    {
        var builder = new StringBuilder()
            .AppendLine("============================================================")
            .AppendLine($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}")
            .AppendLine(context)
            .AppendLine(exception.ToString())
            .AppendLine();

        return Write(builder.ToString());
    }

    public static string LogMessage(string message)
    {
        return Write(
            "============================================================" + Environment.NewLine +
            $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}" + Environment.NewLine +
            message + Environment.NewLine + Environment.NewLine);
    }

    private static string Write(string text)
    {
        foreach (var path in GetCandidatePaths())
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (Writer.TryWrite(path, text))
                {
                    return path;
                }
            }
            catch
            {
                // Try the next location. Startup errors should never create a second crash.
            }
        }

        return "No se pudo escribir el log de errores.";
    }

    private static IEnumerable<string> GetCandidatePaths()
    {
        yield return PreferredLogPath;
        yield return ApplicationPaths.TempCrashLogPath;
        yield return Path.Combine(AppContext.BaseDirectory, "crash.log");
    }
}

public sealed class CrashLogWriter
{
    private readonly object _sync = new();
    private readonly long _maximumActiveLogBytes;
    private readonly int _retainedArchiveCount;
    private readonly Action? _beforeRotation;

    public CrashLogWriter(long maximumActiveLogBytes, int retainedArchiveCount, Action? beforeRotation = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumActiveLogBytes, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(retainedArchiveCount, 1);
        _maximumActiveLogBytes = maximumActiveLogBytes;
        _retainedArchiveCount = retainedArchiveCount;
        _beforeRotation = beforeRotation;
    }

    public bool TryWrite(string activePath, string text)
    {
        lock (_sync)
        {
            try
            {
                var directory = Path.GetDirectoryName(activePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var incomingBytes = Encoding.UTF8.GetByteCount(text);
                if (File.Exists(activePath)
                    && new FileInfo(activePath).Length > 0
                    && new FileInfo(activePath).Length + incomingBytes > _maximumActiveLogBytes)
                {
                    TryRotate(activePath);
                }

                File.AppendAllText(activePath, text, Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    private void TryRotate(string activePath)
    {
        try
        {
            _beforeRotation?.Invoke();
            var oldestPath = ArchivePath(activePath, _retainedArchiveCount);
            if (File.Exists(oldestPath))
            {
                File.Delete(oldestPath);
            }

            for (var index = _retainedArchiveCount - 1; index >= 1; index--)
            {
                var source = ArchivePath(activePath, index);
                if (File.Exists(source))
                {
                    File.Move(source, ArchivePath(activePath, index + 1));
                }
            }

            File.Move(activePath, ArchivePath(activePath, 1));
        }
        catch
        {
            // Preserve the active log when retention maintenance is unavailable.
        }
    }

    private static string ArchivePath(string activePath, int index)
    {
        var directory = Path.GetDirectoryName(activePath) ?? "";
        var name = Path.GetFileNameWithoutExtension(activePath);
        var extension = Path.GetExtension(activePath);
        return Path.Combine(directory, $"{name}.{index}{extension}");
    }
}
