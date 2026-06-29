using System.Diagnostics;
using System.Windows;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
using WpfMessageBox = System.Windows.MessageBox;
using static NeoTwitch.Services.Text.UiTextFormatter;

namespace NeoTwitch;

public partial class MainWindow
{
    private void OpenObsGuide()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = NeoTwitchProduct.Obs.WebSocketGuideUrl,
            UseShellExecute = true
        });
        AddLog("OBS: abriendo guia de obs-websocket.", ActivityLogKind.Obs);
    }

    internal async void ObsSettingsChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        var previousConnectionSignature = ObsConnectionSignature();
        var wasConnected = _obsService.IsConnected;
        SaveGlobalSettingsFromFields();

        if (wasConnected
            && !string.Equals(previousConnectionSignature, ObsConnectionSignature(), StringComparison.Ordinal))
        {
            await _obsService.DisconnectAsync();
            _obsViewModel.ClearScenes();
            AddLog("OBS desconectado porque cambio la configuracion de conexion.", ActivityLogKind.Obs);
        }

        _obsConnectionError = "";
        SaveConfig();
        UpdateObsStatusText();
        UpdateNavigationButtons();
        UpdateSensitiveFieldVisibility();
    }

    private async void ToggleObsConnection()
    {
        if (_isObsConnecting)
        {
            return;
        }

        try
        {
            SaveGlobalSettingsFromFields();
            SaveConfig();

            if (_obsService.IsConnected)
            {
                await _obsService.DisconnectAsync();
                _obsConnectionError = "";
                _obsViewModel.ClearScenes();
                AddLog("OBS desconectado.", ActivityLogKind.Obs);
                UpdateObsStatusText();
                return;
            }

            await ConnectObsAsync();
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "OBS", MessageBoxButton.OK, MessageBoxImage.Warning);
            UpdateObsStatusText();
        }
    }

    private async void TestObsConnection()
    {
        if (_isObsConnecting)
        {
            return;
        }

        try
        {
            SaveGlobalSettingsFromFields();
            SaveConfig();
            if (!_obsService.IsConnected)
            {
                await ConnectObsAsync();
                return;
            }

            _isObsConnecting = true;
            UpdateObsStatusText();
            var result = await _obsService.RefreshScenesAsync(CancellationToken.None);
            ApplyObsResult(result);
            AddLog("OBS: escenas actualizadas.", ActivityLogKind.Obs);
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "OBS", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _isObsConnecting = false;
            UpdateObsStatusText();
        }
    }

    private async Task ConnectObsAsync()
    {
        if (!_config.Obs.Enabled)
        {
            AddLog("OBS esta desactivado en Conexiones.", ActivityLogKind.Obs);
            UpdateObsStatusText();
            return;
        }

        _isObsConnecting = true;
        _obsConnectionError = "";
        UpdateObsStatusText();

        try
        {
            var result = await _obsService.ConnectAsync(_config.Obs, CancellationToken.None);
            ApplyObsResult(result);
            AddLog($"OBS conectado. Escena actual: {FirstNonEmpty(result.CurrentScene, "sin escena")}.", ActivityLogKind.Obs);
        }
        finally
        {
            _isObsConnecting = false;
            UpdateObsStatusText();
        }
    }

}
