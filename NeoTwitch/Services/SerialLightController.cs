using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using NeoTwitch.Models;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace NeoTwitch.Services;

public sealed class SerialLightController : IDisposable
{
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint OpenExisting = 3;
    private const uint FileAttributeNormal = 0x80;
    private const uint PurgeRxAbort = 0x0002;
    private const uint PurgeRxClear = 0x0008;
    private const int AckTimeoutMs = 650;

    private SafeFileHandle? _handle;
    private string _port = "";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private int _baudRate = 115200;
    private bool? _ackSupported;

    public bool HasOpenPort => _handle is { IsInvalid: false, IsClosed: false };

    public bool HasConfirmedAck => HasOpenPort && _ackSupported == true;

    public bool IsCompatibleWithoutAck => HasOpenPort && _ackSupported == false;

    public bool IsReadyForCommands => HasOpenPort && _ackSupported is not null;

    public string CurrentPort => _port;

    public string AckStatusText => _ackSupported switch
    {
        true => "ACK activo",
        false => "modo compatible sin ACK",
        _ => "ACK sin confirmar"
    };

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

        return GetAvailablePorts()
            .Select(port =>
            {
                friendlyNames.TryGetValue(port, out var friendlyName);
                return SerialPortInfo.Create(port, friendlyName);
            })
            .OrderByDescending(port => port.IsLikelyArduino)
            .ThenBy(port => port.SortNumber)
            .ThenBy(port => port.PortName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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

            portName ??= TryFindPortName(friendlyName);
            if (!string.IsNullOrWhiteSpace(portName) && portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            {
                results[portName.ToUpperInvariant()] = CleanFriendlyName(friendlyName, portName);
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

    private static string? TryFindPortName(string? friendlyName)
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

    private static string CleanFriendlyName(string? friendlyName, string portName)
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

    public async Task ConfigureAsync(string port, int baudRate, Action<string> log, CancellationToken cancellationToken)
    {
        var normalizedPort = NormalizePortName(port);

        await _gate.WaitAsync(cancellationToken);
        var openedNewPort = false;

        try
        {
            _baudRate = Math.Clamp(baudRate, ApplicationLimits.MinBaudRate, ApplicationLimits.MaxBaudRate);

            if (string.IsNullOrWhiteSpace(normalizedPort))
            {
                CloseCurrentPort(log);
                return;
            }

            if (HasOpenPort && string.Equals(_port, normalizedPort, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            CloseCurrentPort(log);
            _handle = OpenAndConfigure(normalizedPort, _baudRate);
            _port = normalizedPort;
            _ackSupported = null;
            openedNewPort = true;
            log($"Arduino conectado en {_port} a {_baudRate} baudios.");
        }
        finally
        {
            _gate.Release();
        }

        if (openedNewPort)
        {
            log("Esperando a que Arduino termine de reiniciar el puerto serial...");
            await Task.Delay(2200, cancellationToken);
        }
    }

    public async Task SendAsync(LightCommand command, Action<string> log, CancellationToken cancellationToken)
    {
        await SendLineAsync(command.ToProtocolLine(), log, cancellationToken);
    }

    public async Task StopAsync(IReadOnlyList<LightStripTarget> targets, Action<string> log, CancellationToken cancellationToken)
    {
        await SendLineAsync(LightCommand.ToStopProtocolLine(targets), log, cancellationToken);
    }

    private async Task SendLineAsync(string line, Action<string> log, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (!HasOpenPort || _handle is null)
            {
                log("No hay Arduino conectado.");
                return;
            }

            var commandName = ResolveCommandName(line);
            if (commandName is not null && _ackSupported != false)
            {
                ClearReadBuffer(_handle);
            }

            var bytes = Encoding.ASCII.GetBytes(line);
            if (!WriteFile(_handle, bytes, (uint)bytes.Length, out var written, IntPtr.Zero) || written != bytes.Length)
            {
                var error = Marshal.GetLastWin32Error();
                log($"No se pudo escribir en {_port}: {new Win32Exception(error).Message}");
            }
            else
            {
                log($"Serial {_port}: {line.Trim()}");
                if (commandName is not null && _ackSupported != false)
                {
                    WaitForAck(commandName, log, cancellationToken);
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _handle?.Dispose();
        _handle = null;
        _port = "";
        _ackSupported = null;
        _gate.Dispose();
    }

    private void CloseCurrentPort(Action<string> log)
    {
        if (_handle is null)
        {
            return;
        }

        var oldPort = _port;
        _handle.Dispose();
        _handle = null;
        _port = "";
        _ackSupported = null;

        if (!string.IsNullOrWhiteSpace(oldPort))
        {
            log($"Puerto {oldPort} desconectado.");
        }
    }

    private static SafeFileHandle OpenAndConfigure(string port, int baudRate)
    {
        var handle = CreateFile(
            $@"\\.\{port}",
            GenericRead | GenericWrite,
            0,
            IntPtr.Zero,
            OpenExisting,
            FileAttributeNormal,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"No pude abrir {port}");
        }

        Configure(handle, baudRate);
        return handle;
    }

    private static void Configure(SafeFileHandle handle, int baudRate)
    {
        var dcb = new Dcb
        {
            DcbLength = (uint)Marshal.SizeOf<Dcb>()
        };

        if (!BuildCommDCB($"baud={baudRate} parity=N data=8 stop=1", ref dcb))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "No pude preparar la configuracion serial.");
        }

        if (!SetCommState(handle, ref dcb))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "No pude aplicar la configuracion serial.");
        }

        var timeouts = new CommTimeouts
        {
            ReadIntervalTimeout = 30,
            ReadTotalTimeoutConstant = 80,
            WriteTotalTimeoutConstant = 1000,
            WriteTotalTimeoutMultiplier = 10
        };

        if (!SetCommTimeouts(handle, ref timeouts))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "No pude configurar los tiempos de espera serial.");
        }
    }

    private static string NormalizePortName(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private void WaitForAck(string commandName, Action<string> log, CancellationToken cancellationToken)
    {
        if (_handle is null)
        {
            return;
        }

        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(AckTimeoutMs);

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = ReadLine(_handle, deadline, cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (string.Equals(line, $"ACK|{commandName}", StringComparison.OrdinalIgnoreCase))
            {
                _ackSupported = true;
                log($"Arduino ACK: {commandName} confirmado.");
                return;
            }

            if (line.StartsWith("ERR|", StringComparison.OrdinalIgnoreCase))
            {
                _ackSupported = true;
                log($"Arduino reporto error: {line}.");
                return;
            }

            log($"Arduino respuesta: {line}");
        }

        if (_ackSupported is null)
        {
            _ackSupported = false;
            log("Arduino: no recibi ACK. Sigo en modo compatible; carga el sketch actualizado para confirmaciones.");
            return;
        }

        log($"Arduino: no recibi ACK para {commandName}.");
    }

    private static string? ReadLine(SafeFileHandle handle, DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        var line = new StringBuilder();

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ReadFile(handle, buffer, 1, out var read, IntPtr.Zero))
            {
                return null;
            }

            if (read == 0)
            {
                continue;
            }

            var character = (char)buffer[0];
            if (character == '\n')
            {
                return line.ToString().Trim();
            }

            if (character != '\r')
            {
                line.Append(character);
            }
        }

        return line.Length > 0 ? line.ToString().Trim() : null;
    }

    private static void ClearReadBuffer(SafeFileHandle handle)
    {
        _ = PurgeComm(handle, PurgeRxAbort | PurgeRxClear);
    }

    private static string? ResolveCommandName(string line)
    {
        if (line.StartsWith("FX|", StringComparison.OrdinalIgnoreCase))
        {
            return "FX";
        }

        if (line.StartsWith("STOP|", StringComparison.OrdinalIgnoreCase))
        {
            return "STOP";
        }

        return null;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool BuildCommDCB(string lpDef, ref Dcb lpDcb);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetCommState(SafeFileHandle hFile, ref Dcb lpDcb);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetCommTimeouts(SafeFileHandle hFile, ref CommTimeouts lpCommTimeouts);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool PurgeComm(SafeFileHandle hFile, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteFile(
        SafeFileHandle hFile,
        byte[] lpBuffer,
        uint nNumberOfBytesToWrite,
        out uint lpNumberOfBytesWritten,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadFile(
        SafeFileHandle hFile,
        byte[] lpBuffer,
        uint nNumberOfBytesToRead,
        out uint lpNumberOfBytesRead,
        IntPtr lpOverlapped);

    [StructLayout(LayoutKind.Sequential)]
    private struct Dcb
    {
        public uint DcbLength;
        public uint BaudRate;
        public uint Flags;
        public ushort WReserved;
        public ushort XonLim;
        public ushort XoffLim;
        public byte ByteSize;
        public byte Parity;
        public byte StopBits;
        public sbyte XonChar;
        public sbyte XoffChar;
        public sbyte ErrorChar;
        public sbyte EofChar;
        public sbyte EvtChar;
        public ushort WReserved1;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CommTimeouts
    {
        public uint ReadIntervalTimeout;
        public uint ReadTotalTimeoutMultiplier;
        public uint ReadTotalTimeoutConstant;
        public uint WriteTotalTimeoutMultiplier;
        public uint WriteTotalTimeoutConstant;
    }
}

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
