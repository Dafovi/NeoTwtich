using System.ComponentModel;
using System.Text;
using NeoTwitch.Models;
using NeoTwitch.Services.Lights;
using NeoTwitch.Services.Text;
using Microsoft.Win32.SafeHandles;

namespace NeoTwitch.Services;

public sealed class SerialLightController : IDisposable
{
    private const int AckTimeoutMs = 650;

    private SafeFileHandle? _handle;
    private string _port = "";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IUiTextService _text;
    private int _baudRate = 115200;
    private bool? _ackSupported;

    public SerialLightController(IUiTextService text)
    {
        _text = text;
    }

    public bool HasOpenPort => _handle is { IsInvalid: false, IsClosed: false };

    public bool HasConfirmedAck => HasOpenPort && _ackSupported == true;

    public bool IsCompatibleWithoutAck => HasOpenPort && _ackSupported == false;

    public bool IsReadyForCommands => HasOpenPort && _ackSupported is not null;

    public string CurrentPort => _port;

    public string AckStatusText => _ackSupported switch
    {
        true => _text.Get(UiTextKeys.SerialAckActive),
        false => _text.Get(UiTextKeys.SerialAckCompatible),
        _ => _text.Get(UiTextKeys.SerialAckUnconfirmed)
    };

    public static IReadOnlyList<SerialPortInfo> GetAvailablePortInfos()
    {
        return SerialPortDiscoveryService.GetAvailablePortInfos();
    }

    public static IReadOnlyList<string> GetAvailablePorts()
    {
        return SerialPortDiscoveryService.GetAvailablePorts();
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
            log(_text.Format(UiTextKeys.SerialConnectedLog, _port, _baudRate));
        }
        finally
        {
            _gate.Release();
        }

        if (openedNewPort)
        {
            log(_text.Get(UiTextKeys.SerialRestartWaitLog));
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
                log(_text.Get(UiTextKeys.SerialNoArduinoLog));
                return;
            }

            var commandName = SerialLightProtocol.ResolveCommandName(line);
            if (commandName is not null && _ackSupported != false)
            {
                ClearReadBuffer(_handle);
            }

            var bytes = Encoding.ASCII.GetBytes(line);
            if (!WindowsSerialPortApi.TryWrite(_handle, bytes, out var written, out var error) || written != bytes.Length)
            {
                log(_text.Format(UiTextKeys.SerialWriteFailureLog, _port, new Win32Exception(error).Message));
            }
            else
            {
                log(_text.Format(UiTextKeys.SerialCommandLog, _port, line.Trim()));
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
            log(_text.Format(UiTextKeys.SerialPortDisconnectedLog, oldPort));
        }
    }

    private SafeFileHandle OpenAndConfigure(string port, int baudRate)
    {
        return WindowsSerialPortApi.OpenAndConfigure(port, baudRate, _text);
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

            if (SerialLightProtocol.IsAckFor(line, commandName))
            {
                _ackSupported = true;
                log(_text.Format(UiTextKeys.SerialAckConfirmedLog, commandName));
                return;
            }

            if (SerialLightProtocol.IsError(line))
            {
                _ackSupported = true;
                log(_text.Format(UiTextKeys.SerialReportedErrorLog, line));
                return;
            }

            log(_text.Format(UiTextKeys.SerialResponseLog, line));
        }

        if (_ackSupported is null)
        {
            _ackSupported = false;
            log(_text.Get(UiTextKeys.SerialNoInitialAckLog));
            return;
        }

        log(_text.Format(UiTextKeys.SerialNoCommandAckLog, commandName));
    }

    private static string? ReadLine(SafeFileHandle handle, DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        var line = new StringBuilder();

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!WindowsSerialPortApi.TryRead(handle, buffer, out var read))
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
        WindowsSerialPortApi.ClearReadBuffer(handle);
    }
}

