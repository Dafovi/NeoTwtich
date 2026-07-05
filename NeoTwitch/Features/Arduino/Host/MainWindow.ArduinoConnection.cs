using NeoTwitch.Models;
using NeoTwitch.Services.Text;

namespace NeoTwitch;

public partial class MainWindow
{
    private async void ConnectArduino()
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
            _dialog.ShowWarning("Arduino", ex.Message);
        }
    }

    private async Task ConnectArduinoAsync()
    {
        if (!_config.ArduinoEnabled)
        {
            AddLog(_text.Get(UiTextKeys.ArduinoDisabledLog));
            UpdateStatusText();
            return;
        }

        if (string.IsNullOrWhiteSpace(_config.SerialPort))
        {
            AddLog(_text.Get(UiTextKeys.ArduinoMissingComLog));
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

    private async Task StopLightsAsync(IReadOnlyList<LightStripTarget> targets)
    {
        if (!_config.ArduinoEnabled || !_lightController.HasOpenPort)
        {
            return;
        }

        await _lightController.StopAsync(targets, AddLog, CancellationToken.None);
        UpdateStatusText();
    }
}
