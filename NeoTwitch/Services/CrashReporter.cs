using System.IO;
using System.Text;

namespace NeoTwitch.Services;

public static class CrashReporter
{
    private const string AppFolderName = "NeoTwitch";

    public static string PreferredLogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppFolderName,
        "crash.log");

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

                File.AppendAllText(path, text, Encoding.UTF8);
                return path;
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
        yield return Path.Combine(Path.GetTempPath(), AppFolderName, "crash.log");
        yield return Path.Combine(AppContext.BaseDirectory, "crash.log");
    }
}
