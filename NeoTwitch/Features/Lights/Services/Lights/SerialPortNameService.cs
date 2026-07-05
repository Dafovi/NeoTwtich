namespace NeoTwitch.Services.Lights;

public static class SerialPortNameService
{
    public static string? TryExtractPortName(string? friendlyName)
    {
        if (string.IsNullOrWhiteSpace(friendlyName))
        {
            return null;
        }

        var start = friendlyName.LastIndexOf("(COM", StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return null;
        }

        var end = friendlyName.IndexOf(')', start);
        return end > start ? friendlyName.Substring(start + 1, end - start - 1).ToUpperInvariant() : null;
    }

    public static string CleanFriendlyName(string? friendlyName, string portName)
    {
        if (string.IsNullOrWhiteSpace(friendlyName))
        {
            return portName.ToUpperInvariant();
        }

        var clean = friendlyName.Replace($"({portName})", "", StringComparison.OrdinalIgnoreCase).Trim();
        var semicolon = clean.LastIndexOf(';');
        if (semicolon >= 0 && semicolon < clean.Length - 1)
        {
            clean = clean[(semicolon + 1)..].Trim();
        }

        return string.IsNullOrWhiteSpace(clean) ? portName.ToUpperInvariant() : clean;
    }
}
