using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
using WpfClipboard = System.Windows.Clipboard;
using WpfMessageBox = System.Windows.MessageBox;

namespace NeoTwitch;

public partial class MainWindow
{
    internal async void TwitchButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isTwitchAuthorizing || _isTwitchConnecting)
        {
            return;
        }

        try
        {
            SaveGlobalSettingsFromFields();

            if (_eventSubClient.IsRunning)
            {
                await _eventSubClient.StopAsync();
                _eventSubscriptionSignature = "";
                _streamStatus = null;
                _twitchConnectionError = "";
                AddLog("Twitch desconectado.");
                UpdateStatusText();
                return;
            }

            if (!_config.Token.HasToken || TwitchAuthService.GetMissingScopes(_config.Token).Count > 0)
            {
                await SignInToTwitchAsync();
            }

            await StartTwitchAsync(allowInteractiveReauth: true);
        }
        catch (Exception ex)
        {
            _twitchConnectionError = ex.Message;
            UpdateStatusText();
            AddLog($"Twitch: {ex.Message}");
            WpfMessageBox.Show(this, ex.Message, "Twitch", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    internal void OpenTwitchConsoleButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = NeoTwitchProduct.Links.TwitchDeveloperApps,
            UseShellExecute = true
        });
        AddLog("Twitch Console abierta para revisar el Client ID.", ActivityLogKind.Twitch);
    }

    private void OpenTwitchProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var channel = FirstNonEmpty(_config.Channel.Login, _config.Channel.DisplayName)
            .Trim()
            .TrimStart('@');

        if (string.IsNullOrWhiteSpace(channel))
        {
            WpfMessageBox.Show(
                this,
                "Conecta Twitch primero para abrir el perfil del canal.",
                "Twitch",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = NeoTwitchProduct.Links.TwitchChannel(channel),
            UseShellExecute = true
        });
        AddLog($"Twitch: abriendo perfil de {channel}.", ActivityLogKind.Twitch);
    }

    internal void OpenAlexaConsoleButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = NeoTwitchProduct.Links.AlexaDeveloperConsole,
            UseShellExecute = true
        });
        AddLog("Alexa Developer Console abierta.", ActivityLogKind.Alexa);
    }

    internal void OpenArduinoSketchButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = NeoTwitchProduct.Links.ArduinoSketch,
            UseShellExecute = true
        });
        AddLog("Arduino: abriendo sketch NeoPixel.", ActivityLogKind.Arduino);
    }

    internal void OpenArduinoGuideButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = NeoTwitchProduct.Links.ArduinoGuide,
            UseShellExecute = true
        });
        AddLog("Arduino: abriendo guia de conexion.", ActivityLogKind.Arduino);
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var result = await _versionCheckService.CheckLatestAsync(CancellationToken.None);
            VersionText.Text = $"V{result.CurrentVersion}";

            if (!result.IsUpdateAvailable)
            {
                AddLog($"Version: V{result.CurrentVersion} al dia.");
                return;
            }

            AddLog($"Version: hay una nueva version V{result.LatestVersion}.", ActivityLogKind.Important);
            var installerPath = FindLocalInstallerPath();
            var canUpdateInPlace = !string.IsNullOrWhiteSpace(installerPath);
            var prompt = canUpdateInPlace
                ? $"Hay una nueva version de Neo Twitch.\n\nTu version: V{result.CurrentVersion}\nUltima version: V{result.LatestVersion}\n\nQuieres actualizar ahora? La app se cerrara un momento y el instalador hara el reemplazo."
                : $"Hay una nueva version de Neo Twitch.\n\nTu version: V{result.CurrentVersion}\nUltima version: V{result.LatestVersion}\n\nNo encontre el instalador local. Quieres abrir la pagina de releases para descargarla?";
            var answer = WpfMessageBox.Show(
                this,
                prompt,
                "Actualizacion disponible",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (answer == MessageBoxResult.Yes)
            {
                if (canUpdateInPlace)
                {
                    await LaunchInstallerUpdateAsync(installerPath, result);
                }
                else
                {
                    OpenReleasePage(result.ReleaseUrl);
                }
            }
        }
        catch (Exception ex)
        {
            AddLog($"Version: no pude consultar actualizaciones ({ex.Message}).");
        }
    }

    private async Task LaunchInstallerUpdateAsync(string installerPath, VersionCheckResult result)
    {
        try
        {
            var installPath = AppContext.BaseDirectory.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            var launcherPath = PrepareInstallerLauncher(installerPath);
            Process.Start(new ProcessStartInfo
            {
                FileName = launcherPath,
                Arguments = $"--update --target \"{installPath}\" --version \"V{result.LatestVersion}\"",
                WorkingDirectory = System.IO.Path.GetDirectoryName(launcherPath),
                UseShellExecute = true
            });
            AddLog($"Version: iniciando actualizador a V{result.LatestVersion}.", ActivityLogKind.Important);
            await ExitApplicationAsync();
        }
        catch (Exception ex)
        {
            AddLog($"Version: no pude abrir el actualizador ({ex.Message}).", ActivityLogKind.Important);
            OpenReleasePage(result.ReleaseUrl);
        }
    }

    private static string PrepareInstallerLauncher(string installerPath)
    {
        var updaterDirectory = ApplicationPaths.UpdaterDirectory;
        Directory.CreateDirectory(updaterDirectory);

        var launcherPath = System.IO.Path.Combine(
            updaterDirectory,
            $"{Path.GetFileNameWithoutExtension(NeoTwitchProduct.InstallerExecutableName)}.{Guid.NewGuid():N}.exe");
        File.Copy(installerPath, launcherPath, overwrite: true);
        return launcherPath;
    }

    private static string FindLocalInstallerPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            System.IO.Path.Combine(baseDirectory, NeoTwitchProduct.InstallerExecutableName),
            System.IO.Path.Combine(baseDirectory, "Installer", NeoTwitchProduct.InstallerExecutableName),
            System.IO.Path.Combine(ApplicationPaths.LocalUpdaterDirectory, NeoTwitchProduct.InstallerExecutableName),
        };

        return candidates.FirstOrDefault(File.Exists) ?? string.Empty;
    }

    private static void OpenReleasePage(string releaseUrl)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = releaseUrl,
            UseShellExecute = true
        });
    }

    private async Task SignInToTwitchAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.TwitchClientId))
        {
            throw new InvalidOperationException("Escribe primero el Client ID de Twitch.");
        }

        _isTwitchAuthorizing = true;
        UpdateStatusText();

        try
        {
            var session = await _authService.BeginDeviceFlowAsync(_config.TwitchClientId, CancellationToken.None);
            WpfClipboard.SetText(session.UserCode);
            _authService.OpenVerificationPage(session);
            WpfMessageBox.Show(
                this,
                $"Autoriza la app en Twitch con el codigo {session.UserCode}. El codigo ya quedo copiado al portapapeles.",
                "Login Twitch",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _config.Token = await _authService.PollForTokenAsync(_config.TwitchClientId, session, AddLog, CancellationToken.None);
            _config.Channel = await _authService.GetCurrentUserAsync(_config, CancellationToken.None);
            SaveConfig();
            AddLog($"Twitch autorizado como {_config.Channel.DisplayName}.");
        }
        finally
        {
            _isTwitchAuthorizing = false;
            UpdateStatusText();
        }
    }

    private async Task StartTwitchAsync(bool allowInteractiveReauth = false)
    {
        _isTwitchConnecting = true;
        _twitchConnectionError = "";
        UpdateStatusText();

        try
        {
            var missingScopes = TwitchAuthService.GetMissingScopes(_config.Token);
            if (missingScopes.Count > 0)
            {
                throw new InvalidOperationException($"Twitch necesita autorizar permisos nuevos: {string.Join(", ", missingScopes)}. Presiona Conectar Twitch para iniciar sesion otra vez.");
            }

            try
            {
                await _authService.EnsureValidTokenAsync(_config, AddLog, CancellationToken.None);
            }
            catch (Exception ex) when (allowInteractiveReauth && IsRecoverableTwitchRefreshError(ex))
            {
                AddLog("Twitch necesita autorizar de nuevo porque el token guardado no se pudo refrescar.", ActivityLogKind.Twitch);
                _config.Token = new TwitchTokenInfo();
                _config.Channel = new TwitchChannelInfo();
                SaveConfig();
                await SignInToTwitchAsync();
            }

            if (!_config.Channel.IsReady)
            {
                _config.Channel = await _authService.GetCurrentUserAsync(_config, CancellationToken.None);
                SaveConfig();
            }

            await _eventSubClient.StartAsync();
            _eventSubscriptionSignature = BuildEventSubscriptionSignature();
            await RefreshTwitchStreamStatusAsync();
            AddLog("Twitch escuchando eventos.");
        }
        catch (Exception ex)
        {
            _twitchConnectionError = ex.Message;
            throw;
        }
        finally
        {
            _isTwitchConnecting = false;
            UpdateStatusText();
        }
    }

    private string BuildEventSubscriptionSignature()
    {
        var activeKinds = _config.Rules
            .Where(rule => rule.IsEnabled)
            .Select(rule => rule.EventKind)
            .Where(kind => kind != TwitchEventKind.Test)
            .Distinct()
            .OrderBy(kind => kind)
            .Select(kind => kind.ToString());

        return string.Join("|", activeKinds);
    }

    private void ScheduleTwitchSubscriptionRefreshIfNeeded()
    {
        if (_initializingComponent || _loadingRule || !_eventSubClient.IsRunning)
        {
            return;
        }

        var signature = BuildEventSubscriptionSignature();
        if (string.Equals(signature, _eventSubscriptionSignature, StringComparison.Ordinal))
        {
            return;
        }

        _twitchSubscriptionRefreshDebounce?.Cancel();
        _twitchSubscriptionRefreshDebounce?.Dispose();

        var cts = new CancellationTokenSource();
        _twitchSubscriptionRefreshDebounce = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(900, cts.Token);
                var operation = Dispatcher.InvokeAsync(() => RefreshTwitchSubscriptionsAsync(signature));
                await await operation.Task;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                CrashReporter.Log(ex, "No se pudieron refrescar las suscripciones de Twitch.");
                AddLog($"Twitch: {ex.Message}", ActivityLogKind.Important);
                _ = Dispatcher.InvokeAsync(() =>
                {
                    _twitchConnectionError = ex.Message;
                    UpdateStatusText();
                });
            }
        });
    }

    private async Task RefreshTwitchSubscriptionsAsync(string signature)
    {
        if (!_eventSubClient.IsRunning)
        {
            _eventSubscriptionSignature = signature;
            return;
        }

        AddLog("Twitch: actualizando suscripciones por cambios en reglas.", ActivityLogKind.Twitch);
        await _eventSubClient.StopAsync();
        await _eventSubClient.StartAsync();
        _eventSubscriptionSignature = signature;
        _twitchConnectionError = "";
        AddLog("Twitch: suscripciones actualizadas.", ActivityLogKind.Twitch);
        UpdateStatusText();
    }

    private static bool IsRecoverableTwitchRefreshError(Exception exception)
    {
        var message = exception.Message;
        return message.Contains("No pude refrescar Twitch", StringComparison.OrdinalIgnoreCase)
            && (message.Contains("missing client secret", StringComparison.OrdinalIgnoreCase)
                || message.Contains("invalid client", StringComparison.OrdinalIgnoreCase)
                || message.Contains("invalid refresh token", StringComparison.OrdinalIgnoreCase));
    }

    private async Task RefreshTwitchStreamStatusAsync()
    {
        if (!_config.Token.HasToken || !_config.Channel.IsReady)
        {
            _streamStatus = null;
            UpdateStatusText();
            return;
        }

        try
        {
            await _authService.EnsureValidTokenAsync(_config, AddLog, CancellationToken.None);
            _streamStatus = await _authService.GetStreamStatusAsync(_config, CancellationToken.None);
            _twitchConnectionError = "";
            SaveConfig();
        }
        catch (Exception ex)
        {
            _streamStatus = null;
            _twitchConnectionError = ex.Message;
            AddLog($"Twitch estado: {ex.Message}");
        }

        UpdateStatusText();
    }

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

    private async void ArduinoMonitorTimer_Tick(object? sender, EventArgs e)
    {
        if (_arduinoMonitorBusy || _initializingComponent || _loadingUi || !_config.ArduinoEnabled)
        {
            return;
        }

        var configuredPort = ParsePort(_config.SerialPort);
        if (string.IsNullOrWhiteSpace(configuredPort))
        {
            return;
        }

        _arduinoMonitorBusy = true;
        try
        {
            var availablePorts = SerialLightController.GetAvailablePorts();
            var portPresent = availablePorts.Any(port => string.Equals(port, configuredPort, StringComparison.OrdinalIgnoreCase));

            if (!portPresent)
            {
                if (_lastArduinoPortPresent || _lightController.HasOpenPort)
                {
                    AddLog($"Arduino: {_config.SerialPort} no esta disponible. Marcando como desconectado.", ActivityLogKind.Important);
                    await _lightController.ConfigureAsync("", _config.BaudRate, AddLog, CancellationToken.None);
                    UpdateStatusText();
                }

                _lastArduinoPortPresent = false;
                return;
            }

            if (!_lastArduinoPortPresent)
            {
                AddLog($"Arduino: {_config.SerialPort} volvio a estar disponible.");
            }

            _lastArduinoPortPresent = true;

            if (_lightController.HasOpenPort || !_config.AutoConnectArduino || _isArduinoConnecting)
            {
                return;
            }

            if (DateTimeOffset.Now - _lastArduinoReconnectAttempt < TimeSpan.FromSeconds(8))
            {
                return;
            }

            _lastArduinoReconnectAttempt = DateTimeOffset.Now;
            AddLog($"Arduino: intentando reconectar automaticamente en {_config.SerialPort}.");
            await ConnectArduinoAsync();
            await ApplyBackgroundAsync();
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo monitorear el puerto de Arduino.");
            AddLog($"Arduino monitor: {ex.Message}", ActivityLogKind.Important);
            UpdateStatusText();
        }
        finally
        {
            _arduinoMonitorBusy = false;
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

    private async Task ApplyBackgroundAsync()
    {
        if (!_config.BackgroundEnabled && !_config.BackgroundAlexaEnabled)
        {
            return;
        }

        if (_config.ArduinoEnabled && _config.BackgroundEnabled)
        {
            await ApplyArduinoBackgroundAsync();
        }

        if (_config.BackgroundAlexaEnabled)
        {
            await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOnEventName, "Fondo Alexa encendido");
        }
    }

    private async Task ApplyArduinoBackgroundAsync()
    {
        if (!_config.ArduinoEnabled || !_config.BackgroundEnabled)
        {
            return;
        }

        if (!_lightController.HasOpenPort)
        {
            if (string.IsNullOrWhiteSpace(_config.SerialPort))
            {
                AddLog("No puedo aplicar fondo sin puerto COM.");
                return;
            }

            try
            {
                await ConnectArduinoAsync();
            }
            catch (Exception ex)
            {
                CrashReporter.Log(ex, $"No se pudo conectar Arduino para aplicar fondo en {_config.SerialPort}.");
                AddLog($"Arduino: no pude aplicar fondo en {_config.SerialPort}. Revisa el puerto y conecta manualmente.", ActivityLogKind.Important);
                UpdateStatusText();
                return;
            }
        }

        if (_lightController.HasOpenPort)
        {
            await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
            await Task.Delay(LightStopSettleMs);

            var command = LightCommand.FromBackground(_config);
            await _lightController.SendAsync(command, AddLog, CancellationToken.None);
            UpdateStatusText();
            AddLog($"Fondo aplicado: {DisplayNames.For(command.Pattern)}.");
        }
    }

    private async Task ApplyBackgroundStateAsync()
    {
        if (_effectGate.CurrentCount == 0)
        {
            return;
        }

        await RestoreBackgroundStateAsync(retryArduino: false);
    }

    private async Task RestoreBackgroundStateAsync(bool retryArduino = true)
    {
        await RestoreArduinoBackgroundStateWithRetriesAsync(retryArduino);

        if (_config.BackgroundAlexaTurnOffAfterEvent)
        {
            await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOffEventName, "Fondo Alexa apagado");
        }
        else if (_config.BackgroundAlexaEnabled)
        {
            await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOnEventName, "Fondo Alexa encendido");
        }
    }

    private async Task RestoreArduinoBackgroundStateWithRetriesAsync(bool retryArduino)
    {
        var attempts = _config.ArduinoEnabled && _config.BackgroundEnabled && retryArduino ? 2 : 1;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            if (_config.ArduinoEnabled && _config.BackgroundEnabled)
            {
                await ApplyArduinoBackgroundAsync();
            }
            else if (_config.ArduinoEnabled)
            {
                await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
            }

            if (attempt < attempts)
            {
                await Task.Delay(180);
            }
        }
    }

    private async Task SendBackgroundAlexaEventAsync(string eventName, string title, bool force = false)
    {
        if (!_config.Alexa.IsConfigured
            || (!force && !_config.BackgroundAlexaEnabled && !_config.BackgroundAlexaTurnOffAfterEvent))
        {
            return;
        }

        try
        {
            await _alexaRelayService.SendBackgroundEventAsync(_config, eventName, title, CancellationToken.None);
            _alexaRelayConnected = true;
            AddLog($"Alexa fondo: {eventName}.", ActivityLogKind.Alexa);
        }
        catch (Exception ex)
        {
            _alexaRelayConnected = false;
            CrashReporter.Log(ex, $"No se pudo enviar fondo Alexa '{eventName}'.");
            AddLog($"Alexa fondo: {ex.Message}", ActivityLogKind.Important);
        }
        finally
        {
            UpdateAlexaStatusText();
        }
    }

    private void ScheduleBackgroundApply()
    {
        _backgroundApplyDebounce?.Cancel();
        _backgroundApplyDebounce?.Dispose();

        var cts = new CancellationTokenSource();
        _backgroundApplyDebounce = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(450, cts.Token);
                var operation = Dispatcher.InvokeAsync(ApplyBackgroundStateAsync);
                await await operation.Task;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                CrashReporter.Log(ex, "No se pudo aplicar el fondo programado.");
                AddLog($"Fondo: {ex.Message}");
            }
        });
    }

    private void RefreshPortList(bool choosePreferred)
    {
        var previousPort = ParsePort(PortComboBox.Text);

        try
        {
            _availablePorts = SerialLightController.GetAvailablePortInfos();
            PortComboBox.ItemsSource = _availablePorts;
        }
        catch (Exception ex)
        {
            _availablePorts = [];
            PortComboBox.ItemsSource = _availablePorts;
            CrashReporter.Log(ex, "No se pudo refrescar la lista de puertos COM.");
            AddLog($"No pude refrescar los puertos COM: {ex.Message}");
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

    private async Task StopLightsAsync(IReadOnlyList<LightStripTarget> targets)
    {
        if (!_config.ArduinoEnabled || !_lightController.HasOpenPort)
        {
            return;
        }

        await _lightController.StopAsync(targets, AddLog, CancellationToken.None);
        UpdateStatusText();
    }

    internal void DetectPortsButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshPortList(choosePreferred: true);
        if (_availablePorts.Count == 0)
        {
            AddLog("No encontre puertos COM disponibles.");
            return;
        }

        AddLog($"Puertos detectados: {string.Join(", ", _availablePorts.Select(port => port.DisplayName))}");
    }

    internal void PortComboBox_DropDownOpened(object sender, EventArgs e)
    {
        RefreshPortList(choosePreferred: false);
    }
}
