using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Library;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;
using NeoTwitch.ViewModels.Obs;
using WpfMessageBox = System.Windows.MessageBox;

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

            if (rule.ObsSceneDelayMs > 0)
            {
                await Task.Delay(rule.ObsSceneDelayMs, cancellationToken);
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
                TimeSpan.FromMilliseconds(Math.Clamp(rule.ObsReturnDelayMs, 0, ApplicationLimits.MaxAlertDurationMs)),
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

    private async Task<ObsMediaHideRequest?> SendRuleObsMediaAsync(EventRule rule, CancellationToken cancellationToken)
    {
        if (!rule.SendObsMedia || !_config.Obs.IsConfigured)
        {
            return null;
        }

        var asset = ResolveRuleObsMediaAsset(rule);
        if (asset is null)
        {
            AddLog($"OBS: la regla '{rule.Name}' no tiene un archivo valido para mostrar.", ActivityLogKind.Important);
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

            var sceneName = rule.SendObsScene && !string.IsNullOrWhiteSpace(rule.ObsSceneName)
                ? rule.ObsSceneName.Trim()
                : _obsService.CurrentScene;

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                AddLog("OBS: no hay una escena actual para mostrar el medio.", ActivityLogKind.Important);
                return null;
            }

            var sourceName = rule.ObsMediaKind == ObsMediaKind.Image
                ? NeoTwitchProduct.Obs.AlertImageSourceName
                : NeoTwitchProduct.Obs.AlertVideoSourceName;
            var mediaDuration = MediaRuleAssetService.ResolveRuleMediaDuration(rule, asset);
            var result = await _obsService.ShowMediaSourceAsync(
                sceneName,
                sourceName,
                asset.FilePath,
                rule.ObsMediaKind,
                _config.Obs,
                rule.ObsMediaKind == ObsMediaKind.Video ? _config.VideoVolumePercent : null,
                cancellationToken);

            ApplyObsResult(result);
            WriteObsOverlayState(asset, rule.ObsMediaKind, mediaDuration);
            MarkObsMediaAssetUsed(rule.ObsMediaKind, asset);
            AddLog($"OBS: medio '{asset.DisplayName}' mostrado en '{sceneName}'.", ActivityLogKind.Obs);

            return new ObsMediaHideRequest(
                sceneName,
                sourceName,
                mediaDuration,
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            CrashReporter.Log(ex, $"No se pudo mostrar medio OBS para la regla '{rule.Name}'.");
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            UpdateObsStatusText();
            return null;
        }
    }

    private MediaAssetConfig? ResolveRuleObsMediaAsset(EventRule rule)
    {
        if (!Dispatcher.CheckAccess())
        {
            return Dispatcher.Invoke(() => ResolveRuleObsMediaAsset(rule));
        }

        return MediaRuleAssetService.ResolveRuleMediaAsset(
            rule,
            _config.ImageLibrary,
            _config.VideoLibrary,
            _previewRandom);
    }

    private void MarkObsMediaAssetUsed(ObsMediaKind kind, MediaAssetConfig asset)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => MarkObsMediaAssetUsed(kind, asset));
            return;
        }

        asset.LastUsedAt = DateTimeOffset.Now;
        SaveConfig();
        RefreshMediaLibraryView(kind == ObsMediaKind.Image ? MediaLibraryKind.Image : MediaLibraryKind.Video);
    }

    private async Task HideRuleObsMediaAfterDelayAsync(ObsMediaHideRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var remaining = request.Duration - (DateTimeOffset.UtcNow - request.StartedAt);
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, cancellationToken);
            }

            await HideRuleObsMediaAsync(request, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // The caller hides the media immediately when the alert is cancelled.
        }
    }

    private async Task HideRuleObsMediaAsync(ObsMediaHideRequest request, CancellationToken cancellationToken)
    {
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

        var result = await _obsService.HideSceneSourceAsync(
            request.SceneName,
            request.SourceName,
            cancellationToken);
        ApplyObsResult(result);
        ClearObsOverlayState();
        AddLog($"OBS: medio oculto en '{request.SceneName}'.", ActivityLogKind.Obs);
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
        UpdateObsOverlayFields();

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
        foreach (var scene in result.Scenes)
        {
            _obsSceneRows.Add(new ObsSceneRow(
                scene.Name,
                string.Equals(scene.Name, result.CurrentScene, StringComparison.OrdinalIgnoreCase),
                scene.Name.Length > 24 ? $"{scene.Name[..24]}..." : scene.Name));
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

    internal void ObsOverlaySettingsChanged(object sender, RoutedEventArgs e)
    {
        SaveObsOverlaySettings();
    }

    internal void ObsOverlaySettingsChanged(object sender, TextChangedEventArgs e)
    {
        SaveObsOverlaySettings();
    }

    internal void ObsOverlaySettingsChanged(object sender, SelectionChangedEventArgs e)
    {
        SaveObsOverlaySettings();
    }

    internal void CopyObsOverlayUrlButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var url = BuildObsOverlayUrl();
            System.Windows.Clipboard.SetText(url);
            AddLog("OBS: enlace de overlay copiado.", ActivityLogKind.Obs);
        }
        catch (Exception ex)
        {
            AddLog($"OBS overlay: {ex.Message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "OBS", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SaveObsOverlaySettings()
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        SaveConfig();
        UpdateObsOverlayFields();
    }

    private void UpdateObsOverlayFields()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateObsOverlayFields);
            return;
        }

        if (_initializingComponent)
        {
            return;
        }

        ObsOverlayUrlBox.Text = BuildObsOverlayUrl();
        var customPosition = string.Equals(_config.Obs.OverlayPositionMode, "Custom", StringComparison.OrdinalIgnoreCase);
        ObsOverlayXBox.IsEnabled = customPosition;
        ObsOverlayYBox.IsEnabled = customPosition;
        ObsOverlayXBox.Opacity = customPosition ? 1d : 0.58d;
        ObsOverlayYBox.Opacity = customPosition ? 1d : 0.58d;
    }

    private string BuildObsOverlayUrl()
    {
        return _obsOverlayService.BuildOverlayUrl();
    }

    private void WriteObsOverlayState(MediaAssetConfig asset, ObsMediaKind kind, TimeSpan duration)
    {
        try
        {
            _obsOverlayService.WriteState(asset, kind, _config.Obs, duration);
        }
        catch (Exception ex)
        {
            AddLog($"OBS overlay: {ex.Message}", ActivityLogKind.Important);
        }
    }

    private void ClearObsOverlayState()
    {
        try
        {
            _obsOverlayService.ClearState();
        }
        catch (Exception ex)
        {
            AddLog($"OBS overlay: {ex.Message}", ActivityLogKind.Important);
        }
    }

    private void RefreshObsSceneChoices()
    {
        var selected = RuleObsSceneBox.SelectedValue as string ?? "";
        _obsSceneChoices.Clear();
        _obsSceneChoices.Add(new ObsSceneChoice("", "Mantener escena actual"));
        foreach (var scene in _obsSceneRows)
        {
            _obsSceneChoices.Add(new ObsSceneChoice(scene.Name, scene.Name));
        }

        RuleObsSceneBox.SelectedValue = _obsSceneChoices.Any(choice => string.Equals(choice.Name, selected, StringComparison.OrdinalIgnoreCase))
            ? selected
            : "";
    }
}
