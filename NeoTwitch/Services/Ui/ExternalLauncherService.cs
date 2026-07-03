using System.Diagnostics;

namespace NeoTwitch.Services.Ui;

public sealed class ExternalLauncherService
{
    public void Open(string target)
    {
        Launch(target);
    }

    public void Launch(string fileName, string arguments = "", string? workingDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = true
        });
    }
}
