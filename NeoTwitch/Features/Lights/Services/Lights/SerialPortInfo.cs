namespace NeoTwitch.Services.Lights;

public sealed record SerialPortInfo(string PortName, string FriendlyName, bool IsLikelyArduino, int SortNumber)
{
    public string DisplayName => string.Equals(PortName, FriendlyName, StringComparison.OrdinalIgnoreCase)
        ? PortName
        : $"{PortName} - {FriendlyName}";

    public static SerialPortInfo Create(string portName, string? friendlyName)
    {
        var normalizedPort = portName.ToUpperInvariant();
        var label = string.IsNullOrWhiteSpace(friendlyName) ? normalizedPort : friendlyName.Trim();
        var searchText = $"{normalizedPort} {label}";

        var likelyArduino = searchText.Contains("ARDUINO", StringComparison.OrdinalIgnoreCase)
            || searchText.Contains("CH340", StringComparison.OrdinalIgnoreCase)
            || searchText.Contains("USB-SERIAL", StringComparison.OrdinalIgnoreCase)
            || searchText.Contains("USB SERIAL", StringComparison.OrdinalIgnoreCase)
            || searchText.Contains("CP210", StringComparison.OrdinalIgnoreCase)
            || searchText.Contains("FTDI", StringComparison.OrdinalIgnoreCase);

        return new SerialPortInfo(normalizedPort, label, likelyArduino, ReadComNumber(normalizedPort));
    }

    private static int ReadComNumber(string portName)
    {
        return int.TryParse(portName.Replace("COM", "", StringComparison.OrdinalIgnoreCase), out var number)
            ? number
            : int.MaxValue;
    }
}
