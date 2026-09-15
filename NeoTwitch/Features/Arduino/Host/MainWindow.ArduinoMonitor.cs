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

        _arduinoMonitorBusy = true;
        try
        {
            var configuredPort = ParsePort(_config.SerialPort);
            if (string.IsNullOrWhiteSpace(configuredPort))
            {
                if (_config.AutoConnectArduino && !_isArduinoConnecting)
                {
                    await TryReconnectArduinoFromAvailablePortAsync("Arduino: buscando puerto para reconexion automatica.");
                }

                return;
            }

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
                if (_config.AutoConnectArduino && !_isArduinoConnecting)
                {
                    await TryReconnectArduinoFromAvailablePortAsync($"Arduino: {_config.SerialPort} no esta disponible. Buscando otro puerto.");
                }

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

            if (!IsArduinoReconnectDue())
            {
                return;
            }

            AddLog($"Arduino: intentando reconectar automaticamente en {_config.SerialPort}.");
            await ConnectArduinoAsync();
            ResetArduinoReconnectBackoff();
            await ApplyBackgroundAsync();
        }
        catch (Exception ex)
        {
            ScheduleArduinoReconnectBackoff(ex);
            UpdateStatusText();
        }
        finally
        {
            _arduinoMonitorBusy = false;
        }
    }

    private async Task TryReconnectArduinoFromAvailablePortAsync(string logMessage)
    {
        if (!IsArduinoReconnectDue())
        {
            return;
        }

        AddLog(logMessage, ActivityLogKind.Important);
        if (!TryPrepareArduinoAutoConnectPort(out var selectedPort))
        {
            AddLog(_text.Get(Services.Text.UiTextKeys.StartupArduinoAutoConnectMissingPortLog), ActivityLogKind.Important);
            ScheduleArduinoReconnectBackoff();
            UpdateStatusText();
            return;
        }

        AddLog($"Arduino: intentando reconectar automaticamente en {selectedPort}.");
        await ConnectArduinoAsync();
        ResetArduinoReconnectBackoff();
        await ApplyBackgroundAsync();
    }

    private bool IsArduinoReconnectDue() => _timeProvider.GetUtcNow() >= _nextArduinoReconnectAttempt;

    private void ResetArduinoReconnectBackoff()
    {
        _arduinoReconnectFailures = 0;
        _nextArduinoReconnectAttempt = DateTimeOffset.MinValue;
    }

    private void ScheduleArduinoReconnectBackoff(Exception? exception = null)
    {
        _arduinoReconnectFailures++;
        var delay = ArduinoReconnectBackoffPolicy.DelayAfterFailure(_arduinoReconnectFailures);
        _nextArduinoReconnectAttempt = _timeProvider.GetUtcNow().Add(delay);

        var detail = exception is null ? "No se encontro un puerto disponible." : exception.Message;
        AddLog($"Arduino: {detail} Reintentare en {Math.Ceiling(delay.TotalSeconds)} segundos.", ActivityLogKind.Important);
    }
}
