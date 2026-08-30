using System.Diagnostics;
using System.IO;
using NeoTwitch.Shared;

namespace NeoTwitch.Installer;

internal interface IInstallerProcessProbe
{
    bool IsNeoTwitchRunning();
}

internal sealed class SystemInstallerProcessProbe : IInstallerProcessProbe
{
    public bool IsNeoTwitchRunning()
    {
        var currentProcessId = Environment.ProcessId;
        var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(NeoTwitchProduct.AppExecutableName));
        try
        {
            foreach (var process in processes)
            {
                try
                {
                    if (process.Id != currentProcessId && !process.HasExited)
                    {
                        return true;
                    }
                }
                catch (InvalidOperationException)
                {
                    // The process exited between enumeration and inspection.
                }
            }

            return false;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }
}

internal sealed class InstallerProcessWaiter
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);
    public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(500);

    private readonly IInstallerProcessProbe _probe;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;

    public InstallerProcessWaiter(
        IInstallerProcessProbe? probe = null,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        _probe = probe ?? new SystemInstallerProcessProbe();
        _delayAsync = delayAsync ?? Task.Delay;
    }

    public async Task WaitForExitAsync(
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
    {
        var maximumWait = timeout ?? DefaultTimeout;
        var interval = pollInterval ?? DefaultPollInterval;
        var attempts = Math.Max(1, (int)Math.Ceiling(maximumWait.TotalMilliseconds / interval.TotalMilliseconds));

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool isRunning;
            try
            {
                isRunning = _probe.IsNeoTwitchRunning();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "No se pudo comprobar si Neo Twitch sigue abierto; la instalación se canceló de forma segura.",
                    ex);
            }

            if (!isRunning)
            {
                return;
            }

            progress.Report(new InstallProgress(48, "Esperando a que Neo Twitch se cierre"));
            if (attempt + 1 < attempts)
            {
                await _delayAsync(interval, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Neo Twitch sigue abierto después de {maximumWait.TotalSeconds:0} segundos. "
            + "Cierra la aplicación y vuelve a intentar la instalación.");
    }
}
