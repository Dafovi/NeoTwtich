using System.IO;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.Shared;

namespace NeoTwitch.Services;

public sealed class AppUpdateService
{
    private readonly IExternalLauncherService _externalLauncher;
    private readonly VersionCheckService _versionCheckService;

    public AppUpdateService(IUiTextService text, IExternalLauncherService externalLauncher)
        : this(externalLauncher, new VersionCheckService(text))
    {
    }

    public AppUpdateService(IExternalLauncherService externalLauncher, VersionCheckService versionCheckService)
    {
        _externalLauncher = externalLauncher;
        _versionCheckService = versionCheckService;
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
        _externalLauncher.Launch(
            launcherPath,
            $"--update --target \"{CurrentInstallPath}\" --version \"V{result.LatestVersion}\"",
            Path.GetDirectoryName(launcherPath));
    }

    public void OpenReleasePage(string releaseUrl)
    {
        _externalLauncher.Open(releaseUrl);
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
