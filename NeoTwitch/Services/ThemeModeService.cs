using Microsoft.Win32;

namespace NeoTwitch.Services;

public static class ThemeModeService
{
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "light" => "Light",
            "dark" => "Dark",
            _ => "System"
        };
    }

    public static bool ResolveDarkMode(string? themeMode)
    {
        return Normalize(themeMode) switch
        {
            "Dark" => true,
            "Light" => false,
            _ => IsWindowsAppsDarkMode()
        };
    }

    private static bool IsWindowsAppsDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }
}
