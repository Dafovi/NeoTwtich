using NeoTwitch.Services;
using NeoTwitch.ViewModels.Activity;
using static NeoTwitch.Services.InputValueParser;

namespace NeoTwitch;

public partial class MainWindow
{
    private async void ArduinoMonitorTimer_Tick(object? sender, EventArgs e)
    {
        if (_arduinoMonitorBusy || _initializingComponent || _loadingUi || !_config.ArduinoEnabled)
        {
            return;
        }

        var configuredPort = ParsePort(_config.SerialPort);
        if (string.IsNullOrWhiteSpace(configuredPort))
        {
            return;
        }

        _arduinoMonitorBusy = true;
        try
        {
            var availablePorts = SerialLightController.GetAvailablePorts();
            var portPresent = availablePorts.Any(port => string.Equals(port, configuredPort, StringComparison.OrdinalIgnoreCase));

            if (!portPresent)
            {
                if (_lastArduinoPortPresent || _lightController.HasOpenPort)
                {
                    AddLog($"Arduino: {_config.SerialPort} no esta disponible. Marcando como desconectado.", ActivityLogKind.Important);
                    await _lightController.ConfigureAsync("", _config.BaudRate, AddLog, CancellationToken.None);
                    UpdateStatusText();
                }

                _lastArduinoPortPresent = false;
                return;
            }

            if (!_lastArduinoPortPresent)
            {
                AddLog($"Arduino: {_config.SerialPort} volvio a estar disponible.");
            }

            _lastArduinoPortPresent = true;

            if (_lightController.HasOpenPort || !_config.AutoConnectArduino || _isArduinoConnecting)
            {
                return;
            }

            var now = _timeProvider.GetUtcNow();
            if (now - _lastArduinoReconnectAttempt < TimeSpan.FromSeconds(8))
            {
                return;
            }

            _lastArduinoReconnectAttempt = now;
            AddLog($"Arduino: intentando reconectar automaticamente en {_config.SerialPort}.");
            await ConnectArduinoAsync();
            await ApplyBackgroundAsync();
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo monitorear el puerto de Arduino.");
            AddLog($"Arduino monitor: {ex.Message}", ActivityLogKind.Important);
            UpdateStatusText();
        }
        finally
        {
            _arduinoMonitorBusy = false;
        }
    }
}
