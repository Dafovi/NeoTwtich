using System.Diagnostics;
using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Obs;
using WpfMessageBox = System.Windows.MessageBox;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void OpenObsGuideButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/obsproject/obs-websocket",
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

    private async Task<ObsSceneRestoreRequest?> SendRuleObsSceneAsync(EventRule rule, CancellationToken cancellationToken)
    {
        if (!rule.SendObsScene || !_config.Obs.IsConfigured || string.IsNullOrWhiteSpace(rule.ObsSceneName))
        {
            return null;
        }

        try
        {
            if (!_obsService.IsConnected)
            {
                await ConnectObsAsync();
            }

            if (!_obsService.IsConnected)
            {
                return null;
            }

            var previousScene = _obsService.CurrentScene;
            var targetScene = rule.ObsSceneName.Trim();
            var result = await _obsService.SetCurrentProgramSceneAsync(targetScene, cancellationToken);
            ApplyObsResult(result);
            AddLog($"OBS: escena '{targetScene}' enviada para '{rule.Name}'.", ActivityLogKind.Obs);

            if (!rule.ObsReturnToPreviousScene
                || string.IsNullOrWhiteSpace(previousScene)
                || string.Equals(previousScene, targetScene, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new ObsSceneRestoreRequest(
                previousScene,
                targetScene,
                TimeSpan.FromMilliseconds(Math.Clamp(rule.ObsReturnDelayMs, 0, 600000)),
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            CrashReporter.Log(ex, $"No se pudo enviar escena OBS para la regla '{rule.Name}'.");
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            UpdateObsStatusText();
            return null;
        }
    }

    private async Task RestoreRuleObsSceneAsync(ObsSceneRestoreRequest? restore, bool restoreImmediately)
    {
        if (restore is null)
        {
            return;
        }

        try
        {
            if (!restoreImmediately)
            {
                var remaining = restore.Delay - (DateTimeOffset.UtcNow - restore.StartedAt);
                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining);
                }
            }

            if (!_config.Obs.IsConfigured)
            {
                return;
            }

            if (!_obsService.IsConnected)
            {
                await ConnectObsAsync();
            }

            if (!_obsService.IsConnected)
            {
                return;
            }

            var result = await _obsService.SetCurrentProgramSceneAsync(restore.PreviousScene, CancellationToken.None);
            ApplyObsResult(result);
            AddLog($"OBS: escena restaurada a '{restore.PreviousScene}'.", ActivityLogKind.Obs);
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            CrashReporter.Log(ex, $"No se pudo restaurar la escena OBS '{restore.PreviousScene}'.");
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
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

        var state = !_config.Obs.Enabled
            ? "Desactivado"
            : _isObsConnecting
                ? "Conectando"
                : _obsService.IsConnected
                    ? "Conectado"
                    : !string.IsNullOrWhiteSpace(_obsConnectionError)
                        ? "Revisar conexion"
                        : "Desconectado";

        var statusText = !_config.Obs.Enabled
            ? "OBS desactivado. Las acciones OBS no se mostraran ni ejecutaran."
            : _obsService.IsConnected
                ? $"OBS conectado en {_config.Obs.Host}:{_config.Obs.Port}."
                : !string.IsNullOrWhiteSpace(_obsConnectionError)
                    ? _obsConnectionError
                    : "Conecta OBS Studio para leer escenas y preparar automatizaciones.";

        ObsStatusText.Text = statusText;
        ObsConnectionHelpText.Text = statusText;

        ObsConnectionStateText.Text = state;
        ObsCurrentSceneText.Text = FirstNonEmpty(_obsService.CurrentScene, "Sin escena");
        ObsHostSummaryText.Text = FirstNonEmpty(_config.Obs.Host, "127.0.0.1");
        ObsPortSummaryText.Text = _config.Obs.Port.ToString();
        ObsVersionText.Text = FirstNonEmpty(_obsService.Version, "Sin version");
        ObsSceneCountText.Text = _obsService.Scenes.Count.ToString();
        ObsStudioModeText.Text = _obsService.StudioMode ? "Activado" : "Desactivado";
        ObsScenesList.IsEnabled = _config.Obs.Enabled
            && _obsService.IsConnected
            && !_isObsConnecting
            && !_isObsSceneActionRunning;
        ObsScenesList.Opacity = ObsScenesList.IsEnabled ? 1d : 0.58d;

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
        foreach (var scene in result.Scenes)
        {
            _obsSceneRows.Add(new ObsSceneRow(
                scene.Name,
                string.Equals(scene.Name, result.CurrentScene, StringComparison.OrdinalIgnoreCase),
                scene.Name.Length > 24 ? $"{scene.Name[..24]}..." : scene.Name));
        }

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
}
