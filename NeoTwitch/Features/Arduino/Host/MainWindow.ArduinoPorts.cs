using NeoTwitch.Services;
using NeoTwitch.Services.Text;
using static NeoTwitch.Services.InputValueParser;

namespace NeoTwitch;

public partial class MainWindow
{
    private void RefreshPortList(bool choosePreferred)
    {
        var previousPort = ParsePort(_connectionsViewModel.SerialPort);

        try
        {
            _availablePorts = SerialLightController.GetAvailablePortInfos();
            _connectionsViewModel.UpdatePortChoices(_availablePorts);
        }
        catch (Exception ex)
        {
            _availablePorts = [];
            _connectionsViewModel.UpdatePortChoices(_availablePorts);
            CrashReporter.Log(ex, _text.Get(UiTextKeys.ArduinoPortsRefreshFailureCrash));
            AddLog(_text.Format(UiTextKeys.ArduinoPortsRefreshFailureLog, ex.Message));
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
            _connectionsViewModel.SerialPort = selectedPort;
        }
    }

    private bool TryPrepareArduinoAutoConnectPort(out string selectedPort)
    {
        RefreshPortList(choosePreferred: false);

        var configuredPort = ParsePort(_config.SerialPort);
        if (!string.IsNullOrWhiteSpace(configuredPort)
            && _availablePorts.Any(port => string.Equals(port.PortName, configuredPort, StringComparison.OrdinalIgnoreCase)))
        {
            selectedPort = configuredPort;
            _connectionsViewModel.SerialPort = configuredPort;
            return true;
        }

        selectedPort = ChoosePreferredPort(_availablePorts);
        if (string.IsNullOrWhiteSpace(selectedPort))
        {
            _connectionsViewModel.SerialPort = configuredPort;
            return false;
        }

        if (!string.IsNullOrWhiteSpace(configuredPort)
            && !string.Equals(configuredPort, selectedPort, StringComparison.OrdinalIgnoreCase))
        {
            AddLog($"Arduino: el puerto guardado {configuredPort} no esta disponible. Intentare usar {selectedPort}.");
        }

        _config.SerialPort = selectedPort;
        _connectionsViewModel.SerialPort = selectedPort;
        SaveConfig();
        return true;
    }

    private void DetectPorts()
    {
        RefreshPortList(choosePreferred: true);
        if (_availablePorts.Count == 0)
        {
            AddLog(_text.Get(UiTextKeys.ArduinoPortsNoneDetectedLog));
            return;
        }

        AddLog(_text.Format(UiTextKeys.ArduinoPortsDetectedLog, string.Join(", ", _availablePorts.Select(port => port.DisplayName))));
    }

    internal void PortComboBox_DropDownOpened(object sender, EventArgs e)
    {
        RefreshPortList(choosePreferred: false);
    }
}
