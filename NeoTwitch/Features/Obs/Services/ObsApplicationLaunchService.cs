using System.Diagnostics;
using System.IO;

namespace NeoTwitch.Services.Obs;

public static class ObsApplicationLaunchService
{
    private static readonly string[] ProcessNames = ["obs64", "obs"];

    public static bool TryStartIfNotRunning()
    {
        if (IsRunning())
        {
            return false;
        }

        foreach (var executable in GetCandidatePaths())
        {
            if (!File.Exists(executable))
            {
                continue;
            }

            Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
            return true;
        }

        return false;
    }

    private static bool IsRunning()
    {
        foreach (var name in ProcessNames)
        {
            var processes = Process.GetProcessesByName(name);
            try
            {
                if (processes.Length > 0)
                {
                    return true;
                }
            }
            finally
            {
                foreach (var process in processes)
                {
                    process.Dispose();
                }
            }
        }

        return false;
    }

    private static IEnumerable<string> GetCandidatePaths()
    {
        var relativePath = Path.Combine("obs-studio", "bin", "64bit", "obs64.exe");
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), relativePath);
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), relativePath);
        yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), relativePath);
    }
}
