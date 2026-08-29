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

    private async Task ConnectArduinoAsync(CancellationToken cancellationToken = default)
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
            await _lightController.ConfigureAsync(_config.SerialPort, _config.BaudRate, AddLog, cancellationToken);
            await ConfirmArduinoConnectionAsync(cancellationToken);
        }
        finally
        {
            _isArduinoConnecting = false;
            UpdateStatusText();
        }
    }

    private async Task ConfirmArduinoConnectionAsync(CancellationToken cancellationToken = default)
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

        await _lightController.StopAsync(targets, AddLog, cancellationToken);
    }

    private async Task StopLightsAsync(
        IReadOnlyList<LightStripTarget> targets,
        CancellationToken cancellationToken = default)
    {
        if (!_config.ArduinoEnabled || !_lightController.HasOpenPort)
        {
            return;
        }

        await _lightController.StopAsync(targets, AddLog, cancellationToken);
        UpdateStatusText();
    }
}
