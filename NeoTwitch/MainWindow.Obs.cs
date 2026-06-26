using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Obs;
using NeoTwitch.Services.Status;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;
using NeoTwitch.ViewModels.Obs;
using WpfMessageBox = System.Windows.MessageBox;
using static NeoTwitch.Services.Text.UiTextFormatter;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void OpenObsGuideButton_Click(object sender, RoutedEventArgs e)
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
            _obsSceneRows.Clear();
            AddLog("OBS desconectado porque cambio la configuracion de conexion.", ActivityLogKind.Obs);
        }

        _obsConnectionError = "";
        SaveConfig();
        UpdateObsStatusText();
        UpdateNavigationButtons();
        UpdateSensitiveFieldVisibility();
    }

    internal async void ConnectObsButton_Click(object sender, RoutedEventArgs e)
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
                _obsSceneRows.Clear();
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

    internal async void TestObsButton_Click(object sender, RoutedEventArgs e)
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

    internal async void ObsSceneChangeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ObsSceneRow row })
        {
            return;
        }

        try
        {
            if (!_obsService.IsConnected)
            {
                await ConnectObsAsync();
            }

            var result = await _obsService.SetCurrentProgramSceneAsync(row.Name, CancellationToken.None);
            ApplyObsResult(result);
            AddLog($"OBS: escena cambiada a {row.Name}.", ActivityLogKind.Obs);
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "OBS", MessageBoxButton.OK, MessageBoxImage.Warning);
            UpdateObsStatusText();
        }
    }

    internal async void ObsScenePreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ObsSceneRow row })
        {
            return;
        }

        if (_isObsSceneActionRunning)
        {
            return;
        }

        _isObsSceneActionRunning = true;
        UpdateObsStatusText();

        var previousScene = _obsService.CurrentScene;
        try
        {
            if (!_obsService.IsConnected)
            {
                await ConnectObsAsync();
            }

            if (!_obsService.IsConnected)
            {
                return;
            }

            previousScene = _obsService.CurrentScene;
            var result = await _obsService.SetCurrentProgramSceneAsync(row.Name, CancellationToken.None);
            ApplyObsResult(result);
            AddLog($"OBS: probando escena '{row.Name}' por 5 segundos.", ActivityLogKind.Obs);

            await Task.Delay(TimeSpan.FromSeconds(5));

            if (!string.IsNullOrWhiteSpace(previousScene)
                && !string.Equals(previousScene, row.Name, StringComparison.OrdinalIgnoreCase)
                && _obsService.IsConnected)
            {
                result = await _obsService.SetCurrentProgramSceneAsync(previousScene, CancellationToken.None);
                ApplyObsResult(result);
                AddLog($"OBS: prueba finalizada, regreso a '{previousScene}'.", ActivityLogKind.Obs);
            }
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "OBS", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _isObsSceneActionRunning = false;
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

    private void UpdateObsStatusText()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateObsStatusText);
            return;
        }

        if (_initializingComponent)
        {
            return;
        }

        var status = ObsStatusTextService.Build(
            _config.Obs.Enabled,
            _isObsConnecting,
            _obsService.IsConnected,
            _obsConnectionError,
            _obsService.CurrentScene,
            _config.Obs.Host,
            _config.Obs.Port,
            _obsService.Version,
            _obsService.Scenes.Count,
            _obsService.StudioMode);

        ObsStatusText.Text = status.StatusText;
        ObsConnectionHelpText.Text = status.StatusText;
        UpdateObsOverlayFields();

        ObsConnectionStateText.Text = status.State;
        ObsCurrentSceneText.Text = status.CurrentScene;
        ObsHostSummaryText.Text = status.Host;
        ObsPortSummaryText.Text = status.Port;
        ObsVersionText.Text = status.Version;
        ObsSceneCountText.Text = status.SceneCount;
        ObsStudioModeText.Text = status.StudioMode;
        ObsScenesList.IsEnabled = _config.Obs.Enabled
            && _obsService.IsConnected
            && !_isObsConnecting
            && !_isObsSceneActionRunning;
        ObsScenesList.Opacity = ObsScenesList.IsEnabled ? 1d : 0.58d;

        RefreshMediaLibraryView(MediaLibraryKind.Image);
        RefreshMediaLibraryView(MediaLibraryKind.Video);
        UpdateConnectionButtons();
        RefreshDashboardConnectionStates();
    }

    private void ApplyObsResult(ObsConnectionResult result)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => ApplyObsResult(result));
            return;
        }

        _obsConnectionError = "";
        _obsSceneRows.Clear();
        foreach (var scene in ObsSceneViewService.BuildRows(result.Scenes, result.CurrentScene))
        {
            _obsSceneRows.Add(scene);
        }

        RefreshObsSceneChoices();

        if (RulesList.SelectedItem is EventRule rule
            && !string.IsNullOrWhiteSpace(rule.ObsSceneName)
            && _obsSceneRows.Any(scene => string.Equals(scene.Name, rule.ObsSceneName, StringComparison.OrdinalIgnoreCase)))
        {
            RuleObsSceneBox.SelectedValue = rule.ObsSceneName;
        }

        RefreshRulesView();
        UpdateRuleOptionVisibility();
        UpdateObsStatusText();
    }

    private string ObsConnectionSignature()
    {
        return $"{_config.Obs.Enabled}|{_config.Obs.Host.Trim()}|{_config.Obs.Port}|{_config.Obs.Password}";
    }

    private void RefreshObsSceneChoices()
    {
        var selected = RuleObsSceneBox.SelectedValue as string ?? "";
        var choices = ObsSceneViewService.BuildChoices(_obsSceneRows);
        _obsSceneChoices.Clear();
        foreach (var choice in choices)
        {
            _obsSceneChoices.Add(choice);
        }

        RuleObsSceneBox.SelectedValue = ObsSceneViewService.ResolveSelectedSceneName(selected, _obsSceneChoices);
    }
}
