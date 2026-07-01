using NeoTwitch.Services;
using NeoTwitch.Services.Text;
using static NeoTwitch.Services.InputValueParser;

namespace NeoTwitch;

public partial class MainWindow
{
    private void RefreshPortList(bool choosePreferred)
    {
        var previousPort = ParsePort(PortComboBox.Text);

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
            PortComboBox.SelectedValue = selectedPort;
            PortComboBox.Text = selectedPort;
        }
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
