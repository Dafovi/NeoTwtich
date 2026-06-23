namespace NeoTwitch.Shared;

public static class NeoTwitchProduct
{
    public const string DisplayName = "Neo Twitch";
    public const string AppDataFolderName = "NeoTwitch";
    public const string LegacyAppDataFolderName = "LucesCanjeTwitch";
    public const string AppExecutableName = "NeoTwitch.exe";
    public const string InstallerExecutableName = "NeoTwitch.Installer.exe";
    public const string StartupValueName = DisplayName;
    public const string SingleInstanceMutexName = "NeoTwitch.SingleInstance";
    public const string ShortcutExtension = ".lnk";

    public const string GitHubOwner = "Dafovi";
    public const string GitHubRepository = "NeoTwtich";
    public const string GitHubAppUserAgent = "NeoTwitch";
    public const string GitHubInstallerUserAgent = "NeoTwitchInstaller";

    public static string GitHubRepositoryUrl => $"https://github.com/{GitHubOwner}/{GitHubRepository}";
    public static string ReleasesUrl => $"{GitHubRepositoryUrl}/releases";
    public static string LatestReleaseUrl => $"{ReleasesUrl}/latest";
    public static string LatestReleaseApiUrl => $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepository}/releases/latest";

    public static string AppIconPngResource => "Assets/AppIcon.png";
    public static string AppIconIcoResource => "Assets/AppIcon.ico";
    public static string AppIconPackUri => $"pack://application:,,,/{AppIconPngResource}";

    public static string ShortcutFileName => $"{DisplayName}{ShortcutExtension}";

    public static class Links
    {
        public const string TwitchDeveloperApps = "https://dev.twitch.tv/console/apps";
        public const string AlexaDeveloperConsole = "https://developer.amazon.com/alexa/console/ask";

        public static string ArduinoSketch => $"{GitHubRepositoryUrl}/blob/main/NeoTwitch/Arduino/NeoTwitchNeoPixel/NeoTwitchNeoPixel.ino";
        public static string ArduinoGuide => $"{GitHubRepositoryUrl}#conexion-arduino-y-neopixel";
        public static string TwitchChannel(string channel) => $"https://www.twitch.tv/{Uri.EscapeDataString(channel)}";
    }

    public static class Obs
    {
        public const string WebSocketGuideUrl = "https://github.com/obsproject/obs-websocket";
        public const string AlertImageSourceName = "Neo Twitch - Imagen de alerta";
        public const string AlertVideoSourceName = "Neo Twitch - Video de alerta";
        public const string PreviewImageSourceName = "Neo Twitch - Prueba imagen";
        public const string PreviewVideoSourceName = "Neo Twitch - Prueba video";
        public const string OverlayWindowTitle = "Neo Twitch OBS Overlay";
        public const string OverlayStateAppName = "NeoTwitch";
    }
}
