using System.IO;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.Shared;

namespace NeoTwitch.Services;

public sealed class AppUpdateService
{
    private readonly IExternalLauncherService _externalLauncher;
    private readonly VersionCheckService _versionCheckService;
    private readonly Func<string> _idFactory;
    private readonly string _updaterDirectory;
    private readonly Action<string> _createDirectory;
    private readonly Action<string, string, bool> _copyFile;

    public AppUpdateService(IUiTextService text, IExternalLauncherService externalLauncher)
        : this(externalLauncher, new VersionCheckService(text))
    {
    }

    public AppUpdateService(
        IExternalLauncherService externalLauncher,
        VersionCheckService versionCheckService,
        Func<string>? idFactory = null,
        string? updaterDirectory = null,
        Action<string>? createDirectory = null,
        Action<string, string, bool>? copyFile = null)
    {
        _externalLauncher = externalLauncher;
        _versionCheckService = versionCheckService;
        _idFactory = idFactory ?? (() => Guid.NewGuid().ToString("N"));
        _updaterDirectory = string.IsNullOrWhiteSpace(updaterDirectory)
            ? ApplicationPaths.UpdaterDirectory
            : updaterDirectory;
        _createDirectory = createDirectory ?? (path => Directory.CreateDirectory(path));
        _copyFile = copyFile ?? File.Copy;
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

    private string PrepareInstallerLauncher(string installerPath)
    {
        _createDirectory(_updaterDirectory);

        var launcherPath = Path.Combine(
            _updaterDirectory,
            $"{Path.GetFileNameWithoutExtension(NeoTwitchProduct.InstallerExecutableName)}.{_idFactory()}.exe");
        _copyFile(installerPath, launcherPath, true);
        return launcherPath;
    }
}
