using System.Diagnostics;

namespace NeoTwitch.Services.Ui;

public interface IExternalLauncherService
{
    void Open(string target);

    void Launch(string fileName, string arguments = "", string? workingDirectory = null);
}

public sealed class ExternalLauncherService : IExternalLauncherService
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
