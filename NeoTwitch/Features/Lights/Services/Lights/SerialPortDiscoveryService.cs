using Microsoft.Win32;

namespace NeoTwitch.Services.Lights;

public static class SerialPortDiscoveryService
{
    public static IReadOnlyList<SerialPortInfo> GetAvailablePortInfos()
    {
        Dictionary<string, string> friendlyNames;

        try
        {
            friendlyNames = GetFriendlyPortNames();
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudieron leer los nombres amigables de los puertos COM.");
            friendlyNames = [];
        }

        return BuildPortInfos(GetAvailablePorts(), friendlyNames);
    }

    public static IReadOnlyList<string> GetAvailablePorts()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
            if (key is null)
            {
                return [];
            }

            return key.GetValueNames()
                .Select(name => key.GetValue(name)?.ToString())
                .Where(port => !string.IsNullOrWhiteSpace(port))
                .Select(port => port!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(port => port, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudieron leer los puertos COM disponibles.");
            return [];
        }
    }

    public static IReadOnlyList<SerialPortInfo> BuildPortInfos(
        IEnumerable<string> ports,
        IReadOnlyDictionary<string, string> friendlyNames)
    {
        return ports
            .Where(port => !string.IsNullOrWhiteSpace(port))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(port =>
            {
                var normalizedPort = port.ToUpperInvariant();
                friendlyNames.TryGetValue(normalizedPort, out var friendlyName);
                return SerialPortInfo.Create(normalizedPort, friendlyName);
            })
            .OrderByDescending(port => port.IsLikelyArduino)
            .ThenBy(port => port.SortNumber)
            .ThenBy(port => port.PortName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Dictionary<string, string> GetFriendlyPortNames()
    {
        var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var enumKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum");
            if (enumKey is null)
            {
                return results;
            }

            ScanRegistryForPorts(enumKey, results, 0);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo recorrer el registro de dispositivos USB.");
        }

        return results;
    }

    private static void ScanRegistryForPorts(RegistryKey key, Dictionary<string, string> results, int depth)
    {
        if (depth > 6)
        {
            return;
        }

        try
        {
            var friendlyName = key.GetValue("FriendlyName")?.ToString()
                ?? key.GetValue("DeviceDesc")?.ToString();
            var deviceParameters = key.OpenSubKey("Device Parameters");
            var portName = deviceParameters?.GetValue("PortName")?.ToString();

            portName ??= SerialPortNameService.TryExtractPortName(friendlyName);
            if (!string.IsNullOrWhiteSpace(portName) && portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            {
                results[portName.ToUpperInvariant()] = SerialPortNameService.CleanFriendlyName(friendlyName, portName);
            }

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                using var subKey = key.OpenSubKey(subKeyName);
                if (subKey is not null)
                {
                    ScanRegistryForPorts(subKey, results, depth + 1);
                }
            }
        }
        catch
        {
            // Some USB registry branches are protected. Skip those and keep the ports we can read.
        }
    }
}
