using System.IO;
using NeoTwitch.Shared;

namespace NeoTwitch.Services;

public static class ApplicationPaths
{
    public static string RoamingAppDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        NeoTwitchProduct.AppDataFolderName);

    public static string LegacyRoamingAppDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        NeoTwitchProduct.LegacyAppDataFolderName);

    public static string TempDirectory => Path.Combine(
        Path.GetTempPath(),
        NeoTwitchProduct.AppDataFolderName);

    public static string LocalAppDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        NeoTwitchProduct.AppDataFolderName);

    public static string SettingsPath => Path.Combine(RoamingAppDataDirectory, "settings.json");

    public static string LegacySettingsPath => Path.Combine(LegacyRoamingAppDataDirectory, "settings.json");

    public static string BackupDirectory => Path.Combine(RoamingAppDataDirectory, "backups");

    public static string CrashLogPath => Path.Combine(RoamingAppDataDirectory, "crash.log");

    public static string TempCrashLogPath => Path.Combine(TempDirectory, "crash.log");

    public static string UpdaterDirectory => Path.Combine(TempDirectory, "Updater");

    public static string LocalUpdaterDirectory => Path.Combine(LocalAppDataDirectory, "Updater");

    public static string ObsOverlayDirectory => Path.Combine(RoamingAppDataDirectory, "obs");

    public static string VirtualLightsOverlayDirectory => Path.Combine(RoamingAppDataDirectory, "virtual-lights");
}
