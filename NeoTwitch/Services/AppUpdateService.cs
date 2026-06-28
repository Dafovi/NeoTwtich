using System.Diagnostics;
using System.IO;
using NeoTwitch.Services.Text;
using NeoTwitch.Shared;

namespace NeoTwitch.Services;

public sealed class AppUpdateService
{
    private readonly VersionCheckService _versionCheckService;

    public AppUpdateService(IUiTextService text)
    {
        _versionCheckService = new VersionCheckService(text);
    }

    public Task<VersionCheckResult> CheckLatestAsync(CancellationToken cancellationToken)
    {
        return _versionCheckService.CheckLatestAsync(cancellationToken);
    }

    public static string CurrentInstallPath => AppContext.BaseDirectory.TrimEnd(
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar);

    public string FindLocalInstallerPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDirectory, NeoTwitchProduct.InstallerExecutableName),
            Path.Combine(baseDirectory, "Installer", NeoTwitchProduct.InstallerExecutableName),
            Path.Combine(ApplicationPaths.LocalUpdaterDirectory, NeoTwitchProduct.InstallerExecutableName),
        };

        return candidates.FirstOrDefault(File.Exists) ?? string.Empty;
    }

    public void LaunchInstallerUpdate(string installerPath, VersionCheckResult result)
    {
        var launcherPath = PrepareInstallerLauncher(installerPath);
        Process.Start(new ProcessStartInfo
        {
            FileName = launcherPath,
            Arguments = $"--update --target \"{CurrentInstallPath}\" --version \"V{result.LatestVersion}\"",
            WorkingDirectory = Path.GetDirectoryName(launcherPath),
            UseShellExecute = true
        });
    }

    public void OpenReleasePage(string releaseUrl)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = releaseUrl,
            UseShellExecute = true
        });
    }

    private static string PrepareInstallerLauncher(string installerPath)
    {
        Directory.CreateDirectory(ApplicationPaths.UpdaterDirectory);

        var launcherPath = Path.Combine(
            ApplicationPaths.UpdaterDirectory,
            $"{Path.GetFileNameWithoutExtension(NeoTwitchProduct.InstallerExecutableName)}.{Guid.NewGuid():N}.exe");
        File.Copy(installerPath, launcherPath, overwrite: true);
        return launcherPath;
    }
}
