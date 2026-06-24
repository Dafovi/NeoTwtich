using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Ui;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Status;
using WpfClipboard = System.Windows.Clipboard;
using WpfMessageBox = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;
using static NeoTwitch.Services.Text.UiTextFormatter;
using static NeoTwitch.Services.Ui.UiBrushFactory;

namespace NeoTwitch;

public partial class MainWindow
{
    internal async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveGlobalSettingsFromFields();
        SaveCurrentRuleFromFields();
        SaveCurrentStripFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        await ApplyBackgroundStateAsync();
        AddLog("Configuracion guardada.");
    }

    internal void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveGlobalSettingsFromFields();
            SaveCurrentRuleFromFields();
            SaveCurrentStripFromFields();
            SaveBackgroundFromFields();
            SaveConfig();

            var dialog = new WpfSaveFileDialog
            {
                Title = "Exportar configuracion",
                FileName = $"NeoTwitch-config-{DateTime.Now:yyyyMMdd-HHmmss}.json",
                Filter = "Configuracion Neo Twitch (*.json)|*.json|Todos los archivos (*.*)|*.*",
                AddExtension = true,
                DefaultExt = ".json",
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            _settingsStore.Export(_config, dialog.FileName);
            AddLog($"Configuracion exportada: {dialog.FileName}");
            WpfMessageBox.Show(
                this,
                "Configuracion exportada correctamente.\n\nEste archivo puede incluir tokens, URLs o secretos privados. Guardalo en un lugar seguro.",
                "Configuracion",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo exportar la configuracion.");
            AddLog($"Configuracion: no pude exportar ({ex.Message}).", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "Exportar configuracion", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    internal void CreateBackupButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveGlobalSettingsFromFields();
            SaveCurrentRuleFromFields();
            SaveCurrentStripFromFields();
            SaveBackgroundFromFields();
            SaveConfig();

            Directory.CreateDirectory(_settingsStore.BackupDirectory);
            var backupPath = System.IO.Path.Combine(_settingsStore.BackupDirectory, $"settings-manual-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            _settingsStore.Export(_config, backupPath);
            BackupPathText.Text = $"Ultimo backup manual: {backupPath}";
            AddLog($"Backup creado: {backupPath}");
            WpfMessageBox.Show(this, "Backup creado correctamente.", "Backups", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo crear un backup manual.");
            AddLog($"Backups: no pude crear backup ({ex.Message}).", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "Backups", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    internal async void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = "Importar configuracion",
            Filter = "Configuracion Neo Twitch (*.json)|*.json|Todos los archivos (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var confirm = WpfMessageBox.Show(
            this,
            "Importar esta configuracion reemplazara la configuracion actual. Se creara un backup automatico antes de guardar.\n\nQuieres continuar?",
            "Importar configuracion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            if (_eventSubClient.IsRunning)
            {
                await _eventSubClient.StopAsync();
                _eventSubscriptionSignature = "";
                _streamStatus = null;
            }

            _config = _settingsStore.Import(dialog.FileName);
            LoadConfigIntoUi();
            AddLog($"Configuracion importada: {dialog.FileName}", ActivityLogKind.Important);
            WpfMessageBox.Show(
                this,
                "Configuracion importada correctamente. Revisa Twitch, Arduino y Alexa antes de salir en vivo.",
                "Importar configuracion",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo importar la configuracion.");
            AddLog($"Configuracion: no pude importar ({ex.Message}).", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "Importar configuracion", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    internal async void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = "Restaurar backup",
            Filter = "Backup Neo Twitch (*.json)|*.json|Todos los archivos (*.*)|*.*",
            CheckFileExists = true,
            InitialDirectory = Directory.Exists(_settingsStore.BackupDirectory)
                ? _settingsStore.BackupDirectory
                : System.IO.Path.GetDirectoryName(_settingsStore.SettingsPath)
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var confirm = WpfMessageBox.Show(
            this,
            "Restaurar este backup reemplazara la configuracion actual. Se creara un backup automatico antes de guardar.\n\nQuieres continuar?",
            "Restaurar backup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            if (_eventSubClient.IsRunning)
            {
                await _eventSubClient.StopAsync();
                _eventSubscriptionSignature = "";
                _streamStatus = null;
            }

            _config = _settingsStore.Import(dialog.FileName);
            LoadConfigIntoUi();
            AddLog($"Backup restaurado: {dialog.FileName}", ActivityLogKind.Important);
            WpfMessageBox.Show(
                this,
                "Backup restaurado correctamente. Revisa Twitch, Arduino y Alexa antes de salir en vivo.",
                "Restaurar backup",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo restaurar el backup.");
            AddLog($"Backups: no pude restaurar ({ex.Message}).", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "Restaurar backup", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    internal async void RunDiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveGlobalSettingsFromFields();
            SaveCurrentRuleFromFields();
            SaveCurrentStripFromFields();
            SaveBackgroundFromFields();
            SaveConfig();

            var result = await BuildDiagnosticsReportAsync();
            AddLog(
                result.WarningCount == 0
                    ? "Diagnostico: sin advertencias."
                    : $"Diagnostico: {result.WarningCount} punto(s) por revisar.",
                result.WarningCount == 0 ? ActivityLogKind.Info : ActivityLogKind.Important);
            UpdateSettingsAppState(result.WarningCount == 0
                ? ConnectionVisualState.Connected
                : ConnectionVisualState.Warning);

            ShowDiagnosticsReport(result);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo ejecutar el diagnostico.");
            AddLog($"Diagnostico: {ex.Message}", ActivityLogKind.Important);
            UpdateSettingsAppState(ConnectionVisualState.Disconnected);
            WpfMessageBox.Show(this, ex.Message, "Diagnostico", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateSettingsAppState(ConnectionVisualState state)
    {
        var (text, color, imagePath) = ConnectionStateService.GetAppStateVisual(state);

        SettingsAppStateIcon.Source = PackImageLoader.Load(imagePath);
        SettingsDiagnosticStatusText.Text = text;
        SettingsDiagnosticStatusText.Foreground = FrozenBrushFrom(color);
    }

    private void ShowDiagnosticsReport(DiagnosticResult result)
    {
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        var reportBox = new System.Windows.Controls.TextBox
        {
            Text = result.Report,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new System.Windows.Media.FontFamily("Cascadia Mono, Consolas"),
            FontSize = 12,
            Background = palette.Input,
            Foreground = palette.Text,
            BorderBrush = palette.Border,
            Margin = new Thickness(0, 12, 0, 12)
        };

        var title = new TextBlock
        {
            Text = result.WarningCount == 0
                ? "Diagnostico sin advertencias"
                : $"Diagnostico con {result.WarningCount} punto(s) por revisar",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = palette.Text
        };

        var copyButton = new System.Windows.Controls.Button
        {
            Content = "Copiar reporte",
            Style = (Style)FindResource("PrimaryButton")
        };
        copyButton.Click += (_, _) =>
        {
            WpfClipboard.SetText(result.Report);
            AddLog("Diagnostico copiado al portapapeles.");
        };

        var closeButton = new System.Windows.Controls.Button
        {
            Content = "Cerrar"
        };

        var buttons = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        buttons.Children.Add(copyButton);
        buttons.Children.Add(closeButton);

        var layout = new Grid
        {
            Margin = new Thickness(18)
        };
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.Children.Add(title);
        Grid.SetRow(reportBox, 1);
        layout.Children.Add(reportBox);
        Grid.SetRow(buttons, 2);
        layout.Children.Add(buttons);

        var window = new Window
        {
            Owner = this,
            Title = "Diagnostico Neo Twitch",
            Width = 780,
            Height = 620,
            MinWidth = 560,
            MinHeight = 420,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = palette.Window,
            Icon = Icon,
            Content = layout
        };

        closeButton.Click += (_, _) => window.Close();
        window.ShowDialog();
    }

    private async Task<DiagnosticResult> BuildDiagnosticsReportAsync()
    {
        var body = new StringBuilder();
        var warningCount = 0;

        void Section(string title)
        {
            if (body.Length > 0)
            {
                body.AppendLine();
            }

            body.AppendLine(title);
        }

        void Line(string level, string message)
        {
            body.AppendLine($"{level} {message}");
            if (string.Equals(level, "[REVISAR]", StringComparison.Ordinal))
            {
                warningCount++;
            }
        }

        void Ok(string message) => Line("[OK]", message);
        void Info(string message) => Line("[INFO]", message);
        void Warn(string message) => Line("[REVISAR]", message);

        Section("Version");
        Ok($"Version local: V{NeoTwitchProduct.CurrentVersionText}.");
        try
        {
            using var versionCts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            var version = await _updateService.CheckLatestAsync(versionCts.Token);
            if (version.IsUpdateAvailable)
            {
                Warn($"Hay una version nueva: V{version.LatestVersion}. Releases: {version.ReleaseUrl}");
            }
            else
            {
                Ok("La app esta al dia segun el ultimo release de GitHub.");
            }
        }
        catch (Exception ex)
        {
            Info($"No pude consultar GitHub ahora mismo: {ex.Message}");
        }

        Section("Archivos");
        if (File.Exists(_settingsStore.SettingsPath))
        {
            Ok($"Configuracion encontrada: {_settingsStore.SettingsPath}");
        }
        else
        {
            Warn($"No existe todavia el archivo de configuracion: {_settingsStore.SettingsPath}");
        }

        if (Directory.Exists(_settingsStore.BackupDirectory))
        {
            var backupCount = Directory.EnumerateFiles(_settingsStore.BackupDirectory, "*.json").Count();
            Ok($"Backups automaticos: {backupCount} archivo(s) en {_settingsStore.BackupDirectory}");
        }
        else
        {
            Info($"La carpeta de backups se creara cuando haya cambios guardados: {_settingsStore.BackupDirectory}");
        }

        Section("Twitch");
        if (string.IsNullOrWhiteSpace(_config.TwitchClientId))
        {
            Warn("Falta el Client ID de Twitch.");
        }
        else
        {
            Ok("Client ID configurado.");
        }

        if (_config.Token.HasToken)
        {
            var missingScopes = TwitchAuthService.GetMissingScopes(_config.Token);
            if (missingScopes.Count == 0)
            {
                Ok($"Token guardado con permisos necesarios. Expira: {_config.Token.ExpiresAt.LocalDateTime:g}.");
            }
            else
            {
                Warn($"Twitch necesita reautorizar permisos: {string.Join(", ", missingScopes)}.");
            }
        }
        else
        {
            Warn("No hay sesion de Twitch autorizada.");
        }

        if (_config.Channel.IsReady)
        {
            Ok($"Canal: {FirstNonEmpty(_config.Channel.DisplayName, _config.Channel.Login, "sin nombre")}.");
        }
        else
        {
            Warn("No hay canal de Twitch resuelto todavia.");
        }

        Info(_eventSubClient.IsRunning
            ? "EventSub esta escuchando eventos."
            : "EventSub no esta activo en este momento.");

        if (_streamStatus is { IsLive: true } live)
        {
            Ok($"Canal en directo con {live.ViewerCount} espectadores.");
        }
        else if (_streamStatus is { IsLive: false })
        {
            Info("Canal sin directo activo.");
        }
        else
        {
            Info("Estado del directo no consultado en esta sesion.");
        }

        Section("Arduino");
        if (!_config.ArduinoEnabled)
        {
            Info("Arduino esta desactivado en Conexiones.");
        }
        else
        {
            var ports = SerialLightController.GetAvailablePortInfos();
            if (ports.Count == 0)
            {
                Warn("No encontre puertos COM disponibles.");
            }
            else
            {
                Info($"Puertos detectados: {string.Join(", ", ports.Select(port => port.DisplayName))}.");
            }

            if (string.IsNullOrWhiteSpace(_config.SerialPort))
            {
                Warn("No hay puerto COM configurado para Arduino.");
            }
            else if (ports.Any(port => string.Equals(port.PortName, _config.SerialPort, StringComparison.OrdinalIgnoreCase)))
            {
                Ok($"Puerto configurado disponible: {_config.SerialPort}.");
            }
            else
            {
                Warn($"El puerto configurado {_config.SerialPort} no aparece conectado ahora.");
            }

            Info(_lightController.HasOpenPort
                ? $"Arduino conectado en {_lightController.CurrentPort}. {_lightController.AckStatusText}."
                : "Arduino no esta conectado desde la app.");
            Ok($"{_config.LedStrips.Count} salida(s) LED configurada(s), {_config.LedStrips.Sum(strip => strip.LedCount)} LEDs en total.");
        }

        Section("Alexa");
        if (!_config.Alexa.Enabled)
        {
            Info("Alexa esta desactivada. Esto es correcto si no la quieres usar.");
        }
        else if (_config.Alexa.IsConfigured)
        {
            Ok("Alexa relay configurado.");
        }
        else
        {
            Warn("Alexa esta activa, pero falta una URL valida del relay.");
        }

        if (_config.BackgroundAlexaEnabled)
        {
            Info($"Fondo Alexa encendido: {_config.BackgroundAlexaOnEventName}. Apagado: {_config.BackgroundAlexaOffEventName}.");
        }

        Section("Alertas");
        var activeRules = _config.Rules.Where(rule => rule.IsEnabled).ToArray();
        if (activeRules.Length == 0)
        {
            Warn("No hay reglas activas.");
        }
        else
        {
            Ok($"{activeRules.Length} regla(s) activa(s) de {_config.Rules.Count} total(es).");
        }

        var rulesWithoutAction = activeRules
            .Where(rule => !rule.UseLights && !rule.PlayAudio && !rule.SendChatMessage && !rule.SendAlexaEvent)
            .Select(rule => rule.Name)
            .ToArray();
        if (rulesWithoutAction.Length > 0)
        {
            Warn($"Alertas activas sin acciones: {FormatNameList(rulesWithoutAction)}.");
        }

        var missingAudio = activeRules
            .Where(rule => rule.PlayAudio && !RuleHasValidAudio(rule))
            .Select(rule => rule.Name)
            .ToArray();
        if (missingAudio.Length > 0)
        {
            Warn($"Alertas con audio faltante: {FormatNameList(missingAudio)}.");
        }

        var chatCommandsWithoutCommand = activeRules
            .Where(rule => rule.EventKind == TwitchEventKind.ChatCommand && string.IsNullOrWhiteSpace(rule.ChatCommand))
            .Select(rule => rule.Name)
            .ToArray();
        if (chatCommandsWithoutCommand.Length > 0)
        {
            Warn($"Comandos de chat sin comando escrito: {FormatNameList(chatCommandsWithoutCommand)}.");
        }

        var rulesWithInvalidPins = activeRules
            .Where(rule => rule.UseLights && !string.IsNullOrWhiteSpace(rule.TargetPins) && LightCommand.ParsePins(rule.TargetPins).Count == 0)
            .Select(rule => rule.Name)
            .ToArray();
        if (rulesWithInvalidPins.Length > 0)
        {
            Warn($"Alertas con pines LED no validos: {FormatNameList(rulesWithInvalidPins)}.");
        }

        var activeAlexaRules = activeRules.Count(rule => rule.SendAlexaEvent);
        if (activeAlexaRules > 0 && !_config.Alexa.IsConfigured)
        {
            Warn($"{activeAlexaRules} regla(s) intentan enviar Alexa, pero el relay no esta listo.");
        }

        Section("Fondo y cola");
        Info(_config.BackgroundEnabled
            ? $"Fondo LED activo: {DisplayNames.For(_config.BackgroundPattern)} en pines {FirstNonEmpty(_config.BackgroundTargetPins, "todos")}."
            : "Fondo LED apagado.");
        Info(_config.BackgroundAlexaEnabled
            ? $"Fondo Alexa activo con evento {_config.BackgroundAlexaOnEventName}."
            : "Fondo Alexa apagado.");
        Ok($"Cola: misma regla max {_config.MaxQueuedSameRuleAlerts}, cooldown {_config.SameRuleQueueCooldownMs} ms. Distintas max {_config.MaxQueuedDifferentRuleAlerts}, cooldown {_config.DifferentRuleQueueCooldownMs} ms.");

        var header = new StringBuilder();
        header.AppendLine("Diagnostico Neo Twitch");
        header.AppendLine(warningCount == 0
            ? "Estado general: sin advertencias."
            : $"Estado general: {warningCount} punto(s) por revisar.");
        header.AppendLine($"Fecha: {DateTime.Now:g}");
        header.AppendLine();
        header.Append(body);
        header.AppendLine();
        header.AppendLine("Este diagnostico no ejecuta eventos, no prende luces, no envia chat y no dispara Alexa.");

        return new DiagnosticResult(header.ToString(), warningCount);
    }
}
