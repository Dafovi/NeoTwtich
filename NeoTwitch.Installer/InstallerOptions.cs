using System.IO;

namespace NeoTwitch.Installer;

internal sealed class InstallerOptions
{
    public string InstallPath { get; set; } = DefaultInstallPath;
    public string PackagePath { get; set; } = "";
    public string RequestedVersion { get; set; } = "";
    public bool CreateDesktopShortcut { get; set; } = true;
    public bool CreateStartMenuShortcut { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool LaunchAfterInstall { get; set; } = true;
    public bool IsUpdate { get; set; }

    public static string DefaultInstallPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Neo Twitch");

    public static InstallerOptions FromArgs(string[] args)
    {
        var options = new InstallerOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("--update", StringComparison.OrdinalIgnoreCase))
            {
                options.IsUpdate = true;
                continue;
            }

            if (arg.Equals("--target", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.InstallPath = args[++i];
                continue;
            }

            if (arg.Equals("--package", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.PackagePath = args[++i];
                continue;
            }

            if (arg.Equals("--version", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                options.RequestedVersion = args[++i];
                continue;
            }

            if (arg.Equals("--no-launch", StringComparison.OrdinalIgnoreCase))
            {
                options.LaunchAfterInstall = false;
            }
        }

        return options;
    }
}
