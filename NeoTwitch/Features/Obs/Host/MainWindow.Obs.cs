using System.Windows;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
using static NeoTwitch.Services.Text.UiTextFormatter;

namespace NeoTwitch;

public partial class MainWindow
{
    private void OpenObsGuide()
    {
        _externalLauncher.Open(NeoTwitchProduct.Obs.WebSocketGuideUrl);
        AddLog(_text.Get(Services.Text.UiTextKeys.ObsOpenGuideLog), ActivityLogKind.Obs);
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
            AddLog(_text.Get(Services.Text.UiTextKeys.ObsDisconnectedConfigChangedLog), ActivityLogKind.Obs);
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
                AddLog(_text.Get(Services.Text.UiTextKeys.ObsDisconnectedLog), ActivityLogKind.Obs);
                UpdateObsStatusText();
                return;
            }

            await ConnectObsAsync();
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            _dialog.ShowWarning(_text.Get(Services.Text.UiTextKeys.ObsTitle), ex.Message);
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
            AddLog(_text.Get(Services.Text.UiTextKeys.ObsScenesUpdatedLog), ActivityLogKind.Obs);
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            _dialog.ShowWarning(_text.Get(Services.Text.UiTextKeys.ObsTitle), ex.Message);
        }
        finally
        {
            _isObsConnecting = false;
            UpdateObsStatusText();
        }
    }

    private async Task ConnectObsAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.Obs.Enabled)
        {
            AddLog(_text.Get(Services.Text.UiTextKeys.ObsDisabledLog), ActivityLogKind.Obs);
            UpdateObsStatusText();
            return;
        }

        _isObsConnecting = true;
        _obsConnectionError = "";
        UpdateObsStatusText();

        try
        {
            var result = await _obsService.ConnectAsync(_config.Obs, cancellationToken);
            ApplyObsResult(result);
            AddLog(
                _text.Format(
                    Services.Text.UiTextKeys.ObsConnectedSceneLog,
                    FirstNonEmpty(result.CurrentScene, _text.Get(Services.Text.UiTextKeys.ObsNoScene))),
                ActivityLogKind.Obs);
        }
        finally
        {
            _isObsConnecting = false;
            UpdateObsStatusText();
        }
    }

}
