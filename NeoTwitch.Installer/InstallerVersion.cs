using System.Reflection;

namespace NeoTwitch.Installer;

internal static class InstallerVersion
{
    public static string CurrentVersionText => NormalizeVersionText(
        typeof(InstallerVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? typeof(InstallerVersion).Assembly.GetName().Version?.ToString()
        ?? "0.0.0");

    private static string NormalizeVersionText(string value)
    {
        var normalized = value.Trim();
        var metadataIndex = normalized.IndexOf('+', StringComparison.Ordinal);
        if (metadataIndex >= 0)
        {
            normalized = normalized[..metadataIndex];
        }

        return normalized.TrimStart('v', 'V');
    }
}
