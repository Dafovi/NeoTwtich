using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.ViewModels.Activity;
using WpfMessageBox = System.Windows.MessageBox;
using static NeoTwitch.Services.InputValueParser;

namespace NeoTwitch;

public partial class MainWindow
{
    internal async void ConnectArduinoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isArduinoConnecting)
        {
            return;
        }

        try
        {
            SaveGlobalSettingsFromFields();
            await ConnectArduinoAsync();
            await ApplyBackgroundAsync();
        }
        catch (Exception ex)
        {
            AddLog($"Arduino: {ex.Message}");
            WpfMessageBox.Show(this, ex.Message, "Arduino", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

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

            if (DateTimeOffset.Now - _lastArduinoReconnectAttempt < TimeSpan.FromSeconds(8))
            {
                return;
            }

            _lastArduinoReconnectAttempt = DateTimeOffset.Now;
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

    private async Task ConnectArduinoAsync()
    {
        if (!_config.ArduinoEnabled)
        {
            AddLog("Arduino esta desactivado en Conexiones.");
            UpdateStatusText();
            return;
        }

        if (string.IsNullOrWhiteSpace(_config.SerialPort))
        {
            AddLog("No hay puerto COM configurado.");
            return;
        }

        _isArduinoConnecting = true;
        UpdateStatusText();

        try
        {
            await _lightController.ConfigureAsync(_config.SerialPort, _config.BaudRate, AddLog, CancellationToken.None);
            await ConfirmArduinoConnectionAsync();
        }
        finally
        {
            _isArduinoConnecting = false;
            UpdateStatusText();
        }
    }

    private async Task ConfirmArduinoConnectionAsync()
    {
        if (!_config.ArduinoEnabled || !_lightController.HasOpenPort)
        {
            return;
        }

        var targets = LightCommand.ResolveTargets(_config, "");
        if (targets.Count == 0)
        {
            return;
        }

        await _lightController.StopAsync(targets, AddLog, CancellationToken.None);
    }

    private void RefreshPortList(bool choosePreferred)
    {
        var previousPort = ParsePort(PortComboBox.Text);

        try
        {
            _availablePorts = SerialLightController.GetAvailablePortInfos();
            PortComboBox.ItemsSource = _availablePorts;
        }
        catch (Exception ex)
        {
            _availablePorts = [];
            PortComboBox.ItemsSource = _availablePorts;
            CrashReporter.Log(ex, "No se pudo refrescar la lista de puertos COM.");
            AddLog($"No pude refrescar los puertos COM: {ex.Message}");
        }

        var selectedPort = choosePreferred
            ? ChoosePreferredPort(_availablePorts)
            : _config.SerialPort;

        if (string.IsNullOrWhiteSpace(selectedPort))
        {
            selectedPort = previousPort;
        }

        if (!string.IsNullOrWhiteSpace(selectedPort))
        {
            PortComboBox.SelectedValue = selectedPort;
            PortComboBox.Text = selectedPort;
        }
    }

    private async Task StopLightsAsync(IReadOnlyList<LightStripTarget> targets)
    {
        if (!_config.ArduinoEnabled || !_lightController.HasOpenPort)
        {
            return;
        }

        await _lightController.StopAsync(targets, AddLog, CancellationToken.None);
        UpdateStatusText();
    }

    internal void DetectPortsButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshPortList(choosePreferred: true);
        if (_availablePorts.Count == 0)
        {
            AddLog("No encontre puertos COM disponibles.");
            return;
        }

        AddLog($"Puertos detectados: {string.Join(", ", _availablePorts.Select(port => port.DisplayName))}");
    }

    internal void PortComboBox_DropDownOpened(object sender, EventArgs e)
    {
        RefreshPortList(choosePreferred: false);
    }
}
