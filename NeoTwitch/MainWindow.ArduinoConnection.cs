using System.Windows;
using NeoTwitch.Models;
using WpfMessageBox = System.Windows.MessageBox;

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
