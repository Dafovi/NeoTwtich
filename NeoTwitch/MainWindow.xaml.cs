using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using NeoTwitch.Models;
using NeoTwitch.Services;
using Forms = System.Windows.Forms;
using DrawingIcon = System.Drawing.Icon;
using WpfClipboard = System.Windows.Clipboard;
using WpfMessageBox = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace NeoTwitch;

public partial class MainWindow : Window
{
    private const int DwmWindowAttributeBorderColor = 34;
    private const int DwmWindowAttributeCaptionColor = 35;
    private const int DwmWindowAttributeTextColor = 36;
    private const int AppCaptionColor = 0x0017110B;
    private const int AppCaptionTextColor = 0x00FFFFFF;
    private const int LightStopSettleMs = 120;

    private readonly SettingsStore _settingsStore = new();
    private readonly AudioPlayerService _audioPlayer = new();
    private readonly SerialLightController _lightController = new();
    private readonly TwitchAuthService _authService = new();
    private readonly TwitchChatService _chatService = new();
    private readonly AlexaRelayService _alexaRelayService = new();
    private readonly VersionCheckService _versionCheckService = new();
    private readonly TwitchEventSubClient _eventSubClient;
    private readonly ObservableCollection<ActivityLogEntry> _activity = [];
    private readonly SemaphoreSlim _effectGate = new(1, 1);
    private readonly object _alertQueueSync = new();
    private readonly List<QueuedAlertSlot> _pendingAlertSlots = [];
    private readonly Dictionary<string, DateTimeOffset> _lastRuleStartTimes = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<SerialPortInfo> _availablePorts = [];
    private readonly UiOption<TwitchEventKind>[] _eventOptions =
    [
        new("Nuevo seguidor", TwitchEventKind.Follow),
        new("Nueva suscripcion", TwitchEventKind.Subscription),
        new("Raid recibida", TwitchEventKind.Raid),
        new("Bits", TwitchEventKind.Cheer),
        new("Comando de chat", TwitchEventKind.ChatCommand),
        new("Canje de puntos", TwitchEventKind.ChannelPointRedemption),
        new("Prueba manual", TwitchEventKind.Test)
    ];
    private readonly UiOption<TwitchEventKind>[] _simulatorEventOptions =
    [
        new("Nuevo seguidor", TwitchEventKind.Follow),
        new("Nueva suscripcion", TwitchEventKind.Subscription),
        new("Raid recibida", TwitchEventKind.Raid),
        new("Bits", TwitchEventKind.Cheer),
        new("Comando de chat", TwitchEventKind.ChatCommand),
        new("Canje de puntos", TwitchEventKind.ChannelPointRedemption)
    ];
    private readonly UiOption<LightPattern>[] _patternOptions =
    [
        new("Color fijo", LightPattern.Solid),
        new("Pulso", LightPattern.Pulse),
        new("Arcoiris", LightPattern.Rainbow),
        new("Carrera", LightPattern.Chase),
        new("Teatro", LightPattern.Theater),
        new("Destellos", LightPattern.Sparkle),
        new("Rave", LightPattern.Rave)
    ];
    private readonly UiOption<string>[] _themeModeOptions =
    [
        new("Seguir Windows", "System"),
        new("Claro", "Light"),
        new("Oscuro", "Dark")
    ];

    private AppConfig _config = AppConfig.CreateDefault();
    private bool _initializingComponent;
    private bool _loadingUi;
    private bool _loadingRule;
    private bool _loadingStrip;
    private bool _isExiting;
    private bool _showClientId;
    private bool _showClientSecret;
    private bool _showAlexaRelayUrl;
    private bool _showAlexaAuthToken;
    private BackgroundOutputMode _backgroundOutputMode = BackgroundOutputMode.Arduino;
    private CancellationTokenSource? _backgroundApplyDebounce;
    private CancellationTokenSource? _twitchSubscriptionRefreshDebounce;
    private CancellationTokenSource? _currentEffectCts;
    private string _eventSubscriptionSignature = "";
    private string _runningRuleId = "";
    private string _lastStartedRuleId = "";
    private DateTimeOffset _lastAlertStartAt = DateTimeOffset.MinValue;
    private bool _hasShownTrayNotice;
    private AudioPlayback? _currentPlayback;
    private TwitchStreamStatus? _streamStatus;
    private DrawingIcon? _trayIcon;
    private Forms.NotifyIcon? _notifyIcon;

    public MainWindow()
    {
        _config = _settingsStore.Load();
        _config.ThemeMode = NormalizeThemeMode(_config.ThemeMode);
        _config.DarkMode = ResolveDarkMode(_config.ThemeMode);

        try
        {
            _initializingComponent = true;
            InitializeComponent();
        }
        finally
        {
            _initializingComponent = false;
        }

        _eventSubClient = new TwitchEventSubClient(_authService, () => _config, SaveConfig, AddLog);
        _eventSubClient.EventReceived += EventSubClient_EventReceived;

        _loadingUi = true;
        try
        {
            ActivityList.ItemsSource = _activity;
            MiniActivityList.ItemsSource = _activity;
            EventKindBox.ItemsSource = _eventOptions;
            EventKindBox.DisplayMemberPath = nameof(UiOption<TwitchEventKind>.Label);
            EventKindBox.SelectedValuePath = nameof(UiOption<TwitchEventKind>.Value);
            SimulatorEventKindBox.ItemsSource = _simulatorEventOptions;
            SimulatorEventKindBox.DisplayMemberPath = nameof(UiOption<TwitchEventKind>.Label);
            SimulatorEventKindBox.SelectedValuePath = nameof(UiOption<TwitchEventKind>.Value);
            PatternBox.ItemsSource = _patternOptions;
            PatternBox.DisplayMemberPath = nameof(UiOption<LightPattern>.Label);
            PatternBox.SelectedValuePath = nameof(UiOption<LightPattern>.Value);
            BackgroundPatternBox.ItemsSource = _patternOptions;
            BackgroundPatternBox.DisplayMemberPath = nameof(UiOption<LightPattern>.Label);
            BackgroundPatternBox.SelectedValuePath = nameof(UiOption<LightPattern>.Value);
            ThemeModeBox.ItemsSource = _themeModeOptions;
            ThemeModeBox.DisplayMemberPath = nameof(UiOption<string>.Label);
            ThemeModeBox.SelectedValuePath = nameof(UiOption<string>.Value);
            StripsList.ItemsSource = _config.LedStrips;
            PortComboBox.DisplayMemberPath = nameof(SerialPortInfo.DisplayName);
            PortComboBox.SelectedValuePath = nameof(SerialPortInfo.PortName);
            VersionText.Text = $"V{VersionCheckService.CurrentVersionText}";
            ConfigureNavigationIcons();
            ConfigureActionIcons();
            RefreshPortList(choosePreferred: false);
        }
        finally
        {
            _loadingUi = false;
        }

        CreateTrayIcon();
        LoadConfigIntoUi();
    }

    private void ConfigureNavigationIcons()
    {
        NavSettingsButton.Content = CreateNavigationIcon(IconData("Plug"));
        NavRulesButton.Content = CreateNavigationIcon(IconData("Zap"));
        NavStripsButton.Content = CreateNavigationIcon(IconData("Sun"));
        NavPreferencesButton.Content = CreateNavigationIcon(IconData("Settings"));
        NavActivityButton.Content = CreateNavigationIcon(IconData("Activity"));
    }

    private static System.Windows.Shapes.Path CreateNavigationIcon(string data)
    {
        return CreateIconPath(data, 24, 2);
    }

    private void ConfigureActionIcons()
    {
        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Abrir Twitch Console"] = "ExternalLink",
            ["Detectar"] = "Search",
            ["Conectar"] = "Plug",
            ["Probar Alexa"] = "Play",
            ["Abrir Alexa Console"] = "ExternalLink",
            ["Guardar configuracion"] = "Save",
            ["Nueva"] = "Plus",
            ["Duplicar"] = "Copy",
            ["Eliminar"] = "Trash",
            ["Probar regla"] = "Play",
            ["Parar prueba"] = "Square",
            ["Buscar"] = "Search",
            ["Arduino Tira led ws2812b"] = "Arduino",
            ["Alexa"] = "Alexa",
            ["Aplicar fondo LED"] = "Sun",
            ["Apagar tiras"] = "Power",
            ["Aplicar fondo Alexa"] = "Alexa",
            ["Apagar fondo Alexa"] = "Power",
            ["Exportar configuracion"] = "Upload",
            ["Importar configuracion"] = "Download",
            ["Ejecutar diagnostico"] = "MonitorCheck",
            ["Limpiar actividad"] = "Trash",
            ["Limpiar"] = "Trash"
        };

        foreach (var button in FindVisualChildren<System.Windows.Controls.Button>(this))
        {
            if (IsColorButton(button) || button.Content is not string label)
            {
                continue;
            }

            if (labels.TryGetValue(label.Trim(), out var iconKey))
            {
                SetButtonIcon(button, label.Trim(), iconKey);
            }
        }
    }

    private static void SetButtonIcon(System.Windows.Controls.Button button, string label, string iconKey)
    {
        var panel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.Children.Add(CreateIconPath(IconData(iconKey), 15, 1.9));
        panel.Children.Add(new TextBlock
        {
            Text = label,
            Margin = new Thickness(7, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        });

        button.Content = panel;
    }

    private static System.Windows.Shapes.Path CreateIconPath(string data, double size, double strokeThickness)
    {
        var path = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse(data),
            Width = size,
            Height = size,
            Stretch = Stretch.Uniform,
            StrokeThickness = strokeThickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round
        };

        path.SetBinding(
            Shape.StrokeProperty,
            new System.Windows.Data.Binding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(System.Windows.Controls.Button), 1)
            });

        return path;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private static string IconData(string key)
    {
        return key switch
        {
            "Activity" => "M3,12 L7,12 L10,5 L14,19 L17,12 L21,12",
            "Alexa" => "M12,4 A8,8 0 1 1 12,20 A8,8 0 1 1 12,4 M12,8 A4,4 0 1 1 12,16 A4,4 0 1 1 12,8",
            "Arduino" => "M7,8 C4,8 2,10 2,12 C2,14 4,16 7,16 C9,16 10,14 12,12 C14,10 15,8 17,8 C20,8 22,10 22,12 C22,14 20,16 17,16 C15,16 14,14 12,12 C10,10 9,8 7,8 M5,12 L9,12 M17,10 L17,14 M15,12 L19,12",
            "Copy" => "M8,8 L19,8 L19,19 L8,19 Z M5,15 L4,15 L4,4 L15,4 L15,5",
            "Download" => "M12,3 L12,15 M7,10 L12,15 L17,10 M5,20 L19,20",
            "ExternalLink" => "M14,4 L20,4 L20,10 M20,4 L11,13 M19,14 L19,20 L5,20 L5,6 L11,6",
            "MonitorCheck" => "M4,5 L20,5 L20,16 L4,16 Z M9,21 L15,21 M12,16 L12,21 M8,10 L11,13 L16,8",
            "Play" => "M8,5 L19,12 L8,19 Z",
            "Plug" => "M8,3 L8,9 M16,3 L16,9 M6,9 L18,9 L18,13 C18,16 16,18 13,18 L13,22 M10,22 L10,18 C7,18 5,16 5,13 L5,9",
            "Plus" => "M12,5 L12,19 M5,12 L19,12",
            "Power" => "M12,3 L12,11 M7,6 C5,8 4,10 4,13 C4,17 8,21 12,21 C16,21 20,17 20,13 C20,10 19,8 17,6",
            "Save" => "M5,4 L17,4 L20,7 L20,20 L4,20 L4,4 Z M8,4 L8,10 L16,10 L16,4 M8,20 L8,14 L16,14 L16,20",
            "Search" => "M10.5,5 A5.5,5.5 0 1 1 10.5,16 A5.5,5.5 0 1 1 10.5,5 M15,15 L21,21",
            "Settings" => "M12,8 A4,4 0 1 1 12,16 A4,4 0 1 1 12,8 M12,2 L12,5 M12,19 L12,22 M4.9,4.9 L7,7 M17,17 L19.1,19.1 M2,12 L5,12 M19,12 L22,12 M4.9,19.1 L7,17 M17,7 L19.1,4.9",
            "Square" => "M7,7 L17,7 L17,17 L7,17 Z",
            "Sun" => "M12,7 A5,5 0 1 1 12,17 A5,5 0 1 1 12,7 M12,1 L12,4 M12,20 L12,23 M4.2,4.2 L6.3,6.3 M17.7,17.7 L19.8,19.8 M1,12 L4,12 M20,12 L23,12 M4.2,19.8 L6.3,17.7 M17.7,6.3 L19.8,4.2",
            "Trash" => "M4,7 L20,7 M9,7 L9,5 L15,5 L15,7 M7,7 L8,21 L16,21 L17,7 M10,11 L10,18 M14,11 L14,18",
            "Upload" => "M12,15 L12,3 M7,8 L12,3 L17,8 M5,20 L19,20",
            "Zap" => "M13,2 L4,14 L11,14 L9,22 L20,10 L13,10 Z",
            _ => "M12,5 L12,19 M5,12 L19,12"
        };
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyWindowChromeColor();
        ConfigureActionIcons();
        AddLog("Aplicacion lista.");
        AddLog($"Configuracion: {_settingsStore.SettingsPath}");
        AddLog($"Log de errores: {CrashReporter.PreferredLogPath}");
        if (!string.IsNullOrWhiteSpace(_settingsStore.LastLoadError))
        {
            AddLog($"No pude leer la configuracion anterior: {_settingsStore.LastLoadError}");
        }

        _ = CheckForUpdatesAsync();

        if (_config.StartHidden)
        {
            Hide();
        }

        if (_config.AutoConnectArduino && !string.IsNullOrWhiteSpace(_config.SerialPort))
        {
            try
            {
                await ConnectArduinoAsync();
                await ApplyBackgroundAsync();
            }
            catch (Exception ex)
            {
                CrashReporter.Log(ex, $"No se pudo conectar Arduino automaticamente en {_config.SerialPort}.");
                AddLog($"Arduino: no pude conectar {_config.SerialPort}. Las luces quedan desactivadas hasta reconectar el puerto.", ActivityLogKind.Important);
                UpdateStatusText();
            }
        }

        if (_config.AutoConnectTwitch && _config.Token.HasToken)
        {
            try
            {
                await StartTwitchAsync();
            }
            catch (Exception ex)
            {
                AddLog($"Twitch: {ex.Message}");
            }
        }
    }

    private async void TwitchButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveGlobalSettingsFromFields();

            if (_eventSubClient.IsRunning)
            {
                await _eventSubClient.StopAsync();
                _eventSubscriptionSignature = "";
                _streamStatus = null;
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
            AddLog($"Twitch: {ex.Message}");
            WpfMessageBox.Show(this, ex.Message, "Twitch", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenTwitchConsoleButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://dev.twitch.tv/console/apps",
            UseShellExecute = true
        });
        AddLog("Twitch Console abierta para revisar el Client ID.", ActivityLogKind.Twitch);
    }

    private void OpenAlexaConsoleButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://developer.amazon.com/alexa/console/ask",
            UseShellExecute = true
        });
        AddLog("Alexa Developer Console abierta.", ActivityLogKind.Alexa);
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
            var answer = WpfMessageBox.Show(
                this,
                $"Hay una nueva version de Neo Twitch.\n\nTu version: V{result.CurrentVersion}\nUltima version: V{result.LatestVersion}\n\nQuieres abrir la pagina de releases para descargarla?",
                "Actualizacion disponible",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (answer == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = result.ReleaseUrl,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            AddLog($"Version: no pude consultar actualizaciones ({ex.Message}).");
        }
    }

    private async Task SignInToTwitchAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.TwitchClientId))
        {
            throw new InvalidOperationException("Escribe primero el Client ID de Twitch.");
        }

        TwitchButton.IsEnabled = false;
        TwitchStatusText.Text = "Esperando autorizacion...";

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
            TwitchButton.IsEnabled = true;
            UpdateStatusText();
        }
    }

    private async Task StartTwitchAsync(bool allowInteractiveReauth = false)
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
        UpdateStatusText();
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
            SaveConfig();
        }
        catch (Exception ex)
        {
            _streamStatus = null;
            AddLog($"Twitch estado: {ex.Message}");
        }

        UpdateStatusText();
    }

    private async void ConnectArduinoButton_Click(object sender, RoutedEventArgs e)
    {
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
        if (string.IsNullOrWhiteSpace(_config.SerialPort))
        {
            AddLog("No hay puerto COM configurado.");
            return;
        }

        await _lightController.ConfigureAsync(_config.SerialPort, _config.BaudRate, AddLog, CancellationToken.None);
        UpdateStatusText();
    }

    private async Task ApplyBackgroundAsync()
    {
        if (!_config.BackgroundEnabled && !_config.BackgroundAlexaEnabled)
        {
            return;
        }

        if (_config.BackgroundEnabled)
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
        if (!_config.BackgroundEnabled)
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
        var attempts = _config.BackgroundEnabled && retryArduino ? 2 : 1;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            if (_config.BackgroundEnabled)
            {
                await ApplyArduinoBackgroundAsync();
            }
            else
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
            AddLog($"Alexa fondo: {eventName}.", ActivityLogKind.Alexa);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, $"No se pudo enviar fondo Alexa '{eventName}'.");
            AddLog($"Alexa fondo: {ex.Message}", ActivityLogKind.Important);
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
        if (!_lightController.HasOpenPort)
        {
            return;
        }

        await _lightController.StopAsync(targets, AddLog, CancellationToken.None);
    }

    private void DetectPortsButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshPortList(choosePreferred: true);
        if (_availablePorts.Count == 0)
        {
            AddLog("No encontre puertos COM disponibles.");
            return;
        }

        AddLog($"Puertos detectados: {string.Join(", ", _availablePorts.Select(port => port.DisplayName))}");
    }

    private void PortComboBox_DropDownOpened(object sender, EventArgs e)
    {
        RefreshPortList(choosePreferred: false);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveGlobalSettingsFromFields();
        SaveCurrentRuleFromFields();
        SaveCurrentStripFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        await ApplyBackgroundStateAsync();
        AddLog("Configuracion guardada.");
    }

    private void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
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

    private async void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
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

    private async void RunDiagnosticsButton_Click(object sender, RoutedEventArgs e)
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

            ShowDiagnosticsReport(result);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo ejecutar el diagnostico.");
            AddLog($"Diagnostico: {ex.Message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "Diagnostico", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
        Ok($"Version local: V{VersionCheckService.CurrentVersionText}.");
        try
        {
            using var versionCts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
            var version = await _versionCheckService.CheckLatestAsync(versionCts.Token);
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

        Section("Reglas");
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
            Warn($"Reglas activas sin acciones: {FormatNameList(rulesWithoutAction)}.");
        }

        var missingAudio = activeRules
            .Where(rule => rule.PlayAudio && (string.IsNullOrWhiteSpace(rule.AudioPath) || !File.Exists(rule.AudioPath)))
            .Select(rule => rule.Name)
            .ToArray();
        if (missingAudio.Length > 0)
        {
            Warn($"Reglas con audio faltante: {FormatNameList(missingAudio)}.");
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
            Warn($"Reglas con pines LED no validos: {FormatNameList(rulesWithInvalidPins)}.");
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

    private void AddRuleButton_Click(object sender, RoutedEventArgs e)
    {
        var rule = new EventRule
        {
            Name = "Nueva regla",
            EventKind = TwitchEventKind.Follow,
            MinimumBits = 1,
            UseLights = false,
            PlayAudio = false,
            SendChatMessage = false,
            ChatMessageTemplate = "Gracias @{user}!"
        };

        _config.Rules.Add(rule);
        RulesList.SelectedItem = rule;
        SaveConfig();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
    }

    private void AddStripButton_Click(object sender, RoutedEventArgs e)
    {
        var nextPin = Enumerable.Range(2, 52)
            .FirstOrDefault(pin => _config.LedStrips.All(strip => strip.Pin != pin));

        var strip = new LedStripConfig
        {
            Name = "Nueva tira",
            Pin = nextPin == 0 ? 6 : nextPin,
            LedCount = 30
        };

        _config.LedStrips.Add(strip);
        StripsList.SelectedItem = strip;
        SaveConfig();
    }

    private void DuplicateStripButton_Click(object sender, RoutedEventArgs e)
    {
        if (StripsList.SelectedItem is not LedStripConfig strip)
        {
            return;
        }

        var copy = strip.Duplicate();
        _config.LedStrips.Add(copy);
        StripsList.SelectedItem = copy;
        SaveConfig();
    }

    private void RemoveStripButton_Click(object sender, RoutedEventArgs e)
    {
        if (StripsList.SelectedItem is not LedStripConfig strip)
        {
            return;
        }

        if (_config.LedStrips.Count == 1)
        {
            WpfMessageBox.Show(this, "Deja al menos una tira configurada.", "Luces de fondo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var index = StripsList.SelectedIndex;
        _config.LedStrips.Remove(strip);
        StripsList.SelectedIndex = Math.Clamp(index - 1, 0, _config.LedStrips.Count - 1);
        SaveConfig();
    }

    private void DuplicateRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (RulesList.SelectedItem is not EventRule rule)
        {
            return;
        }

        var copy = rule.Duplicate();
        _config.Rules.Add(copy);
        RulesList.SelectedItem = copy;
        SaveConfig();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
    }

    private void RemoveRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (RulesList.SelectedItem is not EventRule rule)
        {
            return;
        }

        if (WpfMessageBox.Show(this, $"Eliminar la regla '{rule.Name}'?", "Reglas", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        var index = RulesList.SelectedIndex;
        _config.Rules.Remove(rule);
        if (_config.Rules.Count > 0)
        {
            RulesList.SelectedIndex = Math.Clamp(index - 1, 0, _config.Rules.Count - 1);
        }
        else
        {
            LoadSelectedRuleIntoUi();
        }

        SaveConfig();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
    }

    private async void TestRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (RulesList.SelectedItem is not EventRule rule)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        SaveCurrentRuleFromFields();
        var simulatedEvent = BuildSimulatedEvent(rule);

        if (!rule.Matches(simulatedEvent))
        {
            var message = $"La regla '{rule.Name}' no se ejecutaria con esta simulacion. Regla: {DisplayNames.For(rule.EventKind)}. Simulacion: {DisplayNames.For(simulatedEvent.Kind)}.";
            AddLog($"Simulador: {message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, message, "Simulador de eventos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!ValidateSimulatedRun(rule, simulatedEvent))
        {
            return;
        }
        AddLog(
            $"Simulando {DescribeSimulatedEvent(simulatedEvent)} para regla '{rule.Name}'. Acciones: {DescribeRuleActions(rule)}.",
            ActivityLogKind.Event);

        await RunRuleAsync(rule, simulatedEvent);
    }

    private TwitchEvent BuildSimulatedEvent(EventRule rule)
    {
        var kind = SimulatorEventKindBox.SelectedValue is TwitchEventKind selectedKind
            ? selectedKind
            : rule.EventKind == TwitchEventKind.Test
                ? TwitchEventKind.Follow
                : rule.EventKind;
        var userName = FirstNonEmpty(SimulatorUserBox.Text.Trim(), "Prueba");
        var bits = ParseInt(SimulatorBitsBox.Text, Math.Max(1, rule.MinimumBits), 1, 1_000_000);
        var viewers = ParseInt(SimulatorViewersBox.Text, 18, 1, 1_000_000);
        var rewardTitle = FirstNonEmpty(SimulatorRewardBox.Text.Trim(), rule.CustomRewardTitle, "Canje de prueba");
        var message = FirstNonEmpty(SimulatorMessageBox.Text.Trim(), rule.ChatCommand, "!baile mensaje de prueba");

        return new TwitchEvent
        {
            Kind = kind,
            UserName = userName,
            RewardTitle = kind == TwitchEventKind.ChannelPointRedemption ? rewardTitle : null,
            Bits = kind == TwitchEventKind.Cheer ? bits : null,
            ViewerCount = kind == TwitchEventKind.Raid ? viewers : null,
            Message = kind == TwitchEventKind.ChatCommand ? message : "Mensaje de prueba",
            RawType = "simulator",
            Title = $"Simulacion: {DisplayNames.For(kind)} de {userName}"
        };
    }

    private bool ValidateSimulatedRun(EventRule rule, TwitchEvent twitchEvent)
    {
        if (rule.PlayAudio && (string.IsNullOrWhiteSpace(rule.AudioPath) || !File.Exists(rule.AudioPath)))
        {
            var message = $"El audio de '{rule.Name}' no existe o no esta configurado.";
            AddLog($"Simulador: {message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, message, "Simulador de eventos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (rule.UseLights && !_lightController.HasOpenPort)
        {
            AddLog(
                string.IsNullOrWhiteSpace(_config.SerialPort)
                    ? "Simulador: la regla usa luces, pero no hay puerto COM configurado."
                    : $"Simulador: la regla usa luces, pero Arduino no esta conectado ahora ({_config.SerialPort}).",
                ActivityLogKind.Important);
        }

        if (rule.UseLights && !string.IsNullOrWhiteSpace(rule.TargetPins) && LightCommand.ParsePins(rule.TargetPins).Count == 0)
        {
            AddLog($"Simulador: los pines de la regla '{rule.Name}' no son validos.", ActivityLogKind.Important);
        }

        if (rule.SendAlexaEvent && !_config.Alexa.IsConfigured)
        {
            AddLog("Simulador: Alexa esta activada en la regla, pero el relay no esta configurado.", ActivityLogKind.Important);
        }

        if (rule.EventKind == TwitchEventKind.ChatCommand
            && !EventRuleMatchesChatCommand(rule, twitchEvent.Message))
        {
            AddLog("Simulador: el mensaje no empieza con el comando configurado.", ActivityLogKind.Important);
        }

        return true;
    }

    private static bool EventRuleMatchesChatCommand(EventRule rule, string? message)
    {
        if (rule.EventKind != TwitchEventKind.ChatCommand)
        {
            return true;
        }

        var command = rule.ChatCommand.Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        if (!command.StartsWith('!'))
        {
            command = $"!{command}";
        }

        var firstToken = message?.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.Equals(firstToken, command, StringComparison.OrdinalIgnoreCase);
    }

    private static string DescribeSimulatedEvent(TwitchEvent twitchEvent)
    {
        var user = FirstNonEmpty(twitchEvent.UserName ?? "", "Prueba");
        return twitchEvent.Kind switch
        {
            TwitchEventKind.Cheer => $"{twitchEvent.Bits ?? 0} bits de {user}",
            TwitchEventKind.Raid => $"raid de {user} con {twitchEvent.ViewerCount ?? 0} viewers",
            TwitchEventKind.ChannelPointRedemption => $"canje '{FirstNonEmpty(twitchEvent.RewardTitle ?? "", "Canje de prueba")}' de {user}",
            TwitchEventKind.ChatCommand => $"comando de chat de {user}: {FirstNonEmpty(twitchEvent.Message ?? "", "sin mensaje")}",
            _ => $"{DisplayNames.For(twitchEvent.Kind)} de {user}"
        };
    }

    private static string DescribeRuleActions(EventRule rule)
    {
        List<string> actions = [];

        if (rule.UseLights)
        {
            actions.Add("luces");
        }

        if (rule.PlayAudio)
        {
            actions.Add("audio");
        }

        if (rule.SendChatMessage)
        {
            actions.Add("chat");
        }

        if (rule.SendAlexaEvent)
        {
            actions.Add("Alexa");
        }

        return actions.Count == 0 ? "ninguna accion activa" : string.Join(", ", actions);
    }

    private void BrowseAudioButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Filter = "Audio|*.wav;*.mp3;*.wma;*.aac;*.m4a|Todos los archivos|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        AudioPathBox.Text = dialog.FileName;
        SaveCurrentRuleFromFields();
    }

    private void RulesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        LoadSelectedRuleIntoUi();
    }

    private void StripsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        LoadSelectedStripIntoUi();
    }

    private void GlobalSettingsChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        SaveConfig();
        UpdateSensitiveFieldVisibility();
        UpdateStatusText();
    }

    private void AlexaSettingsChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        SaveConfig();
        UpdateAlexaStatusText();
        UpdateSensitiveFieldVisibility();
        UpdateRuleOptionVisibility();
    }

    private async void TestAlexaButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveGlobalSettingsFromFields();
            SaveConfig();
            await _alexaRelayService.SendTestEventAsync(_config, CancellationToken.None);
            AddLog("Alexa: evento de prueba enviado.", ActivityLogKind.Alexa);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo enviar la prueba de Alexa.");
            AddLog($"Alexa: {ex.Message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "Alexa", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            UpdateAlexaStatusText();
        }
    }

    private void RuleFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingRule)
        {
            return;
        }

        SaveCurrentRuleFromFields();
        SaveConfig();
        UpdateRuleOptionVisibility();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
    }

    private void StripFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingStrip)
        {
            return;
        }

        SaveCurrentStripFromFields();
        SaveConfig();
        ScheduleBackgroundApply();
    }

    private void BackgroundFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveBackgroundFromFields();
        SaveConfig();
        UpdateBackgroundOptionVisibility();
        ScheduleBackgroundApply();
    }

    private void ThemeModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        ApplyTheme();
        SaveConfig();
    }

    private void ToggleClientIdVisibility_Click(object sender, RoutedEventArgs e)
    {
        _showClientId = !_showClientId;
        UpdateSensitiveFieldVisibility();
    }

    private void ToggleClientSecretVisibility_Click(object sender, RoutedEventArgs e)
    {
        _showClientSecret = !_showClientSecret;
        UpdateSensitiveFieldVisibility();
    }

    private void ToggleAlexaRelayUrlVisibility_Click(object sender, RoutedEventArgs e)
    {
        _showAlexaRelayUrl = !_showAlexaRelayUrl;
        UpdateSensitiveFieldVisibility();
    }

    private void ToggleAlexaAuthTokenVisibility_Click(object sender, RoutedEventArgs e)
    {
        _showAlexaAuthToken = !_showAlexaAuthToken;
        UpdateSensitiveFieldVisibility();
    }

    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, MainTabs) || _initializingComponent)
        {
            return;
        }

        UpdateNavigationButtons();
        ConfigureActionIcons();
        _ = Dispatcher.BeginInvoke(ApplyTheme, DispatcherPriority.Loaded);
        _ = Dispatcher.BeginInvoke(ApplyTheme, DispatcherPriority.ContextIdle);
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string tag }
            || !int.TryParse(tag, out var selectedIndex)
            || selectedIndex < 0
            || selectedIndex >= MainTabs.Items.Count)
        {
            return;
        }

        MainTabs.SelectedIndex = selectedIndex;
        UpdateNavigationButtons();
    }

    private void ArduinoOutputButton_Click(object sender, RoutedEventArgs e)
    {
        _backgroundOutputMode = BackgroundOutputMode.Arduino;
        if (StripsList.SelectedItem is null && _config.LedStrips.Count > 0)
        {
            StripsList.SelectedIndex = 0;
        }

        ApplyBackgroundOutputMode();
    }

    private void AlexaOutputButton_Click(object sender, RoutedEventArgs e)
    {
        _backgroundOutputMode = BackgroundOutputMode.Alexa;
        ApplyBackgroundOutputMode();
    }

    private async void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        await ExitApplicationAsync();
    }

    private void PrimaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(PrimaryColorBox);
    }

    private void SecondaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(SecondaryColorBox);
    }

    private void TertiaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(TertiaryColorBox);
    }

    private void BackgroundPrimaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(BackgroundPrimaryColorBox);
    }

    private void BackgroundSecondaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(BackgroundSecondaryColorBox);
    }

    private void BackgroundTertiaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(BackgroundTertiaryColorBox);
    }

    private async void ApplyArduinoBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SaveGlobalSettingsFromFields();
        SaveCurrentStripFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        await ApplyArduinoBackgroundAsync();
    }

    private async void ApplyAlexaBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SaveGlobalSettingsFromFields();
        SaveBackgroundFromFields();
        SaveConfig();

        if (_config.BackgroundAlexaEnabled)
        {
            await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOnEventName, "Fondo Alexa encendido", force: true);
        }
    }

    private async void StopArduinoBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SaveGlobalSettingsFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
    }

    private async void StopAlexaBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SaveGlobalSettingsFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOffEventName, "Fondo Alexa apagado", force: true);
    }

    private async void StopTestButton_Click(object sender, RoutedEventArgs e)
    {
        await StopCurrentEffectAsync();
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        _activity.Clear();
    }

    private async void EventSubClient_EventReceived(TwitchEvent twitchEvent)
    {
        var matchingRules = ResolveMatchingRules(twitchEvent);
        if (matchingRules.Length == 0)
        {
            if (twitchEvent.Kind != TwitchEventKind.ChatCommand)
            {
                AddLog(twitchEvent.Title, ActivityLogKind.Event);
                AddLog("El evento no coincide con reglas activas.");
            }

            return;
        }

        AddLog(twitchEvent.Title, ActivityLogKind.Event);

        foreach (var rule in matchingRules)
        {
            await QueueAndRunRuleAsync(rule, twitchEvent);
        }
    }

    private async Task QueueAndRunRuleAsync(EventRule rule, TwitchEvent twitchEvent)
    {
        var slot = TryReserveAlertSlot(rule, twitchEvent, out var reason);
        if (slot is null)
        {
            AddLog($"Cola: descarte '{rule.Name}'. {reason}", ActivityLogKind.Important);
            return;
        }

        await RunRuleAsync(rule, twitchEvent, queueSlot: slot);
    }

    private QueuedAlertSlot? TryReserveAlertSlot(EventRule rule, TwitchEvent twitchEvent, out string reason)
    {
        lock (_alertQueueSync)
        {
            var now = DateTimeOffset.UtcNow;
            var busy = _effectGate.CurrentCount == 0 || _pendingAlertSlots.Count > 0;
            var samePending = _pendingAlertSlots.Count(slot => string.Equals(slot.RuleId, rule.Id, StringComparison.OrdinalIgnoreCase));
            var sameLimit = Math.Clamp(_config.MaxQueuedSameRuleAlerts, 0, 100);
            var differentLimit = Math.Clamp(_config.MaxQueuedDifferentRuleAlerts, 0, 100);

            if (busy)
            {
                if (samePending >= sameLimit)
                {
                    reason = sameLimit == 0
                        ? "No se permite acumular alertas repetidas."
                        : $"Ya hay {samePending} alerta(s) repetida(s) esperando.";
                    return null;
                }

                var isDifferentFromRunning = !string.IsNullOrWhiteSpace(_runningRuleId)
                    && !string.Equals(_runningRuleId, rule.Id, StringComparison.OrdinalIgnoreCase);
                var isDifferentWhileManualIsRunning = string.IsNullOrWhiteSpace(_runningRuleId)
                    && _effectGate.CurrentCount == 0;
                var isDifferentWhileQueueIsWaiting = string.IsNullOrWhiteSpace(_runningRuleId)
                    && _pendingAlertSlots.Count > 0;
                var differentPending = _pendingAlertSlots.Count(slot => !string.Equals(slot.RuleId, rule.Id, StringComparison.OrdinalIgnoreCase));

                if ((isDifferentFromRunning || isDifferentWhileManualIsRunning || isDifferentWhileQueueIsWaiting) && differentPending >= differentLimit)
                {
                    reason = differentLimit == 0
                        ? "No se permite acumular alertas distintas mientras otra esta activa."
                        : $"Ya hay {differentPending} alerta(s) distinta(s) esperando.";
                    return null;
                }
            }

            var sameCooldownMs = Math.Clamp(_config.SameRuleQueueCooldownMs, 0, 600000);
            if (sameCooldownMs > 0
                && _lastRuleStartTimes.TryGetValue(rule.Id, out var lastSameStart)
                && now - lastSameStart < TimeSpan.FromMilliseconds(sameCooldownMs))
            {
                var remainingMs = sameCooldownMs - (int)(now - lastSameStart).TotalMilliseconds;
                reason = $"Repetida en enfriamiento por {Math.Max(0, remainingMs)} ms.";
                return null;
            }

            var differentCooldownMs = Math.Clamp(_config.DifferentRuleQueueCooldownMs, 0, 600000);
            if (differentCooldownMs > 0
                && !string.IsNullOrWhiteSpace(_lastStartedRuleId)
                && !string.Equals(_lastStartedRuleId, rule.Id, StringComparison.OrdinalIgnoreCase)
                && now - _lastAlertStartAt < TimeSpan.FromMilliseconds(differentCooldownMs))
            {
                var remainingMs = differentCooldownMs - (int)(now - _lastAlertStartAt).TotalMilliseconds;
                reason = $"Distinta en enfriamiento por {Math.Max(0, remainingMs)} ms.";
                return null;
            }

            var slot = new QueuedAlertSlot(Guid.NewGuid().ToString("N"), rule.Id, rule.Name, twitchEvent.Kind);
            _pendingAlertSlots.Add(slot);
            reason = "";
            return slot;
        }
    }

    private void MarkQueuedAlertStarted(QueuedAlertSlot? slot)
    {
        if (slot is null)
        {
            return;
        }

        lock (_alertQueueSync)
        {
            _pendingAlertSlots.RemoveAll(candidate => string.Equals(candidate.Id, slot.Id, StringComparison.OrdinalIgnoreCase));
            var now = DateTimeOffset.UtcNow;
            _runningRuleId = slot.RuleId;
            _lastStartedRuleId = slot.RuleId;
            _lastAlertStartAt = now;
            _lastRuleStartTimes[slot.RuleId] = now;
        }
    }

    private void MarkQueuedAlertFinished(QueuedAlertSlot? slot)
    {
        if (slot is null)
        {
            return;
        }

        lock (_alertQueueSync)
        {
            if (string.Equals(_runningRuleId, slot.RuleId, StringComparison.OrdinalIgnoreCase))
            {
                _runningRuleId = "";
            }
        }
    }

    private EventRule[] ResolveMatchingRules(TwitchEvent twitchEvent)
    {
        var matchingRules = _config.Rules
            .Where(rule => rule.Matches(twitchEvent))
            .ToArray();

        if (twitchEvent.Kind != TwitchEventKind.Cheer || matchingRules.Length == 0)
        {
            return matchingRules;
        }

        var highestThreshold = matchingRules.Max(rule => rule.MinimumBits);
        return matchingRules
            .Where(rule => rule.MinimumBits == highestThreshold)
            .ToArray();
    }

    private async Task SendRuleChatMessageAsync(EventRule rule, TwitchEvent twitchEvent)
    {
        if (!rule.SendChatMessage)
        {
            return;
        }

        var message = TwitchChatService.FormatMessage(rule.ChatMessageTemplate, twitchEvent);
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            await _authService.EnsureValidTokenAsync(_config, AddLog, CancellationToken.None);
            SaveConfig();
            await _chatService.SendMessageAsync(_config, message, CancellationToken.None);
            AddLog($"Chat enviado: {message}", ActivityLogKind.Twitch);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, $"No se pudo enviar mensaje de chat para la regla '{rule.Name}'.");
            AddLog($"Chat: {ex.Message}");
        }
    }

    private async Task SendRuleAlexaEventAsync(EventRule rule, TwitchEvent twitchEvent)
    {
        if (!rule.SendAlexaEvent || !_config.Alexa.IsConfigured)
        {
            return;
        }

        try
        {
            await _alexaRelayService.SendRuleEventAsync(_config, rule, twitchEvent, CancellationToken.None);
            AddLog($"Alexa: evento enviado para '{rule.Name}'.", ActivityLogKind.Alexa);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, $"No se pudo enviar evento Alexa para la regla '{rule.Name}'.");
            AddLog($"Alexa: {ex.Message}", ActivityLogKind.Important);
        }
    }

    private async Task RunRuleAsync(
        EventRule rule,
        TwitchEvent twitchEvent,
        bool sendChatMessage = true,
        bool sendAlexaEvent = true,
        QueuedAlertSlot? queueSlot = null)
    {
        await _effectGate.WaitAsync();
        MarkQueuedAlertStarted(queueSlot);
        var effectCts = new CancellationTokenSource();
        _currentEffectCts = effectCts;
        var wasCancelled = false;
        var shouldRestoreBackground = false;

        try
        {
            if (sendChatMessage)
            {
                _ = SendRuleChatMessageAsync(rule, twitchEvent);
            }

            if (sendAlexaEvent)
            {
                _ = SendRuleAlexaEventAsync(rule, twitchEvent);
            }

            AudioPlayback? playback = null;
            if (rule.PlayAudio)
            {
                playback = await _audioPlayer.PrepareAsync(rule.AudioPath, _config.AlertVolumePercent, AddLog);
                _currentPlayback = playback;
            }

            if (!rule.UseLights)
            {
                playback?.Play();
                if (playback is not null)
                {
                    await playback.Completion.WaitAsync(effectCts.Token);
                }

                return;
            }

            if (rule.UseLights && !_lightController.HasOpenPort && !string.IsNullOrWhiteSpace(_config.SerialPort))
            {
                await ConnectArduinoAsync();
            }

            shouldRestoreBackground = true;
            var targets = LightCommand.ResolveTargets(_config, rule.TargetPins);
            if (rule.UseLights)
            {
                await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
                await Task.Delay(LightStopSettleMs);
            }

            var audioDuration = playback?.Duration;
            var syncedDurationMs = audioDuration is { TotalMilliseconds: > 0 }
                ? (int)Math.Round(audioDuration.Value.TotalMilliseconds)
                : (int?)null;

            LightCommand? command = null;
            if (rule.UseLights)
            {
                command = LightCommand.FromRule(rule, _config, syncedDurationMs);
                await _lightController.SendAsync(command, AddLog, CancellationToken.None);
            }

            playback?.Play();

            if (playback is not null)
            {
                await playback.Completion.WaitAsync(effectCts.Token);
            }
            else if (command is not null)
            {
                await Task.Delay(command.DurationMs, effectCts.Token);
            }
            else
            {
                await Task.Delay(500, effectCts.Token);
            }

            if (command is not null)
            {
                await StopLightsAsync(targets);
                AddLog($"Luces: {DisplayNames.For(rule.Pattern)} por {command.DurationMs} ms para {DisplayNames.For(twitchEvent.Kind)}.");
            }
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
            AddLog("Prueba detenida.");
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, $"Error ejecutando la regla '{rule.Name}'.");
            AddLog($"Regla '{rule.Name}': {ex.Message}");
        }
        finally
        {
            _currentPlayback = null;
            if (ReferenceEquals(_currentEffectCts, effectCts))
            {
                _currentEffectCts = null;
            }

            if (shouldRestoreBackground || wasCancelled)
            {
                try
                {
                    await RestoreBackgroundStateAsync();
                }
                catch (Exception ex)
                {
                    CrashReporter.Log(ex, "No se pudo restaurar el fondo despues de una regla.");
                    AddLog($"Fondo: {ex.Message}");
                }
            }

            effectCts.Dispose();
            MarkQueuedAlertFinished(queueSlot);
            _effectGate.Release();
        }
    }

    private async Task StopCurrentEffectAsync()
    {
        _currentEffectCts?.Cancel();
        _currentPlayback?.Stop();
        await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
        await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOffEventName, "Fondo Alexa apagado");

        if (_effectGate.CurrentCount > 0)
        {
            await ApplyBackgroundStateAsync();
        }
    }

    private void LoadConfigIntoUi()
    {
        _loadingUi = true;

        try
        {
            ClientIdBox.Text = _config.TwitchClientId;
            ClientSecretBox.Text = _config.TwitchClientSecret;
            PortComboBox.SelectedValue = _config.SerialPort;
            PortComboBox.Text = _config.SerialPort;
            BaudRateBox.Text = _config.BaudRate.ToString();
            AutoTwitchCheck.IsChecked = _config.AutoConnectTwitch;
            AutoArduinoCheck.IsChecked = _config.AutoConnectArduino;
            StartHiddenCheck.IsChecked = _config.StartHidden;
            ThemeModeBox.SelectedValue = _config.ThemeMode;
            CloseToTrayCheck.IsChecked = _config.CloseToTray;
            AlertVolumeSlider.Value = _config.AlertVolumePercent;
            MaxQueuedSameRuleAlertsBox.Text = _config.MaxQueuedSameRuleAlerts.ToString();
            SameRuleQueueCooldownBox.Text = _config.SameRuleQueueCooldownMs.ToString();
            MaxQueuedDifferentRuleAlertsBox.Text = _config.MaxQueuedDifferentRuleAlerts.ToString();
            DifferentRuleQueueCooldownBox.Text = _config.DifferentRuleQueueCooldownMs.ToString();
            AlexaEnabledCheck.IsChecked = _config.Alexa.Enabled;
            AlexaRelayUrlBox.Text = _config.Alexa.RelayUrl;
            AlexaAuthTokenBox.Text = _config.Alexa.AuthToken;
            BackgroundEnabledCheck.IsChecked = _config.BackgroundEnabled;
            BackgroundAlexaEnabledCheck.IsChecked = _config.BackgroundAlexaEnabled;
            BackgroundAlexaTurnOffAfterEventCheck.IsChecked = _config.BackgroundAlexaTurnOffAfterEvent;
            BackgroundAlexaOnEventBox.Text = _config.BackgroundAlexaOnEventName;
            BackgroundAlexaOffEventBox.Text = _config.BackgroundAlexaOffEventName;
            BackgroundPinsBox.Text = _config.BackgroundTargetPins;
            BackgroundPatternBox.SelectedValue = _config.BackgroundPattern;
            BackgroundPrimaryColorBox.Text = LightCommand.NormalizeColor(_config.BackgroundPrimaryColor);
            BackgroundSecondaryColorBox.Text = LightCommand.NormalizeColor(_config.BackgroundSecondaryColor);
            BackgroundTertiaryColorBox.Text = LightCommand.NormalizeColor(_config.BackgroundTertiaryColor);
            BackgroundBrightnessSlider.Value = _config.BackgroundBrightness;
            BackgroundCycleSlider.Value = _config.BackgroundCycleMs;
            BackgroundStepSlider.Value = _config.BackgroundStepMs;
            RulesList.ItemsSource = _config.Rules;
            StripsList.ItemsSource = _config.LedStrips;
            SettingsPathText.Text = _settingsStore.SettingsPath;
            BackupPathText.Text = $"Backups automaticos: {_settingsStore.BackupDirectory}";

            if (_config.Rules.Count > 0)
            {
                RulesList.SelectedIndex = 0;
            }

            if (_config.LedStrips.Count > 0)
            {
                StripsList.SelectedIndex = 0;
            }

            LoadSelectedRuleIntoUi();
            LoadSelectedStripIntoUi();
            UpdateBackgroundOptionVisibility();
            ApplyBackgroundOutputMode();
            UpdateAlexaStatusText();
            UpdateSensitiveFieldVisibility();
            ApplyTheme();
            UpdateStatusText();
        }
        finally
        {
            _loadingUi = false;
        }
    }

    private void LoadSelectedRuleIntoUi()
    {
        _loadingRule = true;

        try
        {
            RuleEditorPanel.IsEnabled = RulesList.SelectedItem is EventRule;

            if (RulesList.SelectedItem is not EventRule rule)
            {
                return;
            }

            RuleEnabledCheck.IsChecked = rule.IsEnabled;
            RuleNameBox.Text = rule.Name;
            EventKindBox.SelectedValue = rule.EventKind;
            SimulatorEventKindBox.SelectedValue = rule.EventKind == TwitchEventKind.Test
                ? TwitchEventKind.Follow
                : rule.EventKind;
            SimulatorUserBox.Text = FirstNonEmpty(SimulatorUserBox.Text, "Prueba");
            SimulatorBitsBox.Text = Math.Max(1, rule.MinimumBits).ToString();
            SimulatorViewersBox.Text = FirstNonEmpty(SimulatorViewersBox.Text, "18");
            SimulatorRewardBox.Text = FirstNonEmpty(rule.CustomRewardTitle, SimulatorRewardBox.Text, "Canje de prueba");
            SimulatorMessageBox.Text = rule.EventKind == TwitchEventKind.ChatCommand
                ? FirstNonEmpty(rule.ChatCommand, SimulatorMessageBox.Text, "!baile mensaje de prueba")
                : FirstNonEmpty(SimulatorMessageBox.Text, "Mensaje de prueba");
            RewardTitleBox.Text = rule.CustomRewardTitle;
            ChatCommandBox.Text = rule.ChatCommand;
            MinimumBitsBox.Text = rule.MinimumBits.ToString();
            ChatMessageCheck.IsChecked = rule.SendChatMessage;
            ChatMessageBox.Text = rule.ChatMessageTemplate;
            AlexaEventCheck.IsChecked = rule.SendAlexaEvent;
            UseLightsCheck.IsChecked = rule.UseLights;
            PlayAudioCheck.IsChecked = rule.PlayAudio;
            AudioPathBox.Text = rule.AudioPath;
            PatternBox.SelectedValue = rule.Pattern;
            TargetPinsBox.Text = rule.TargetPins;
            PrimaryColorBox.Text = LightCommand.NormalizeColor(rule.PrimaryColor);
            SecondaryColorBox.Text = LightCommand.NormalizeColor(rule.SecondaryColor);
            TertiaryColorBox.Text = LightCommand.NormalizeColor(rule.TertiaryColor);
            BrightnessSlider.Value = rule.Brightness;
            DurationSlider.Value = rule.DurationMs;
            CycleSlider.Value = rule.CycleMs;
            StepSlider.Value = rule.StepMs;
            UpdateColorButtons();
            UpdateSliderLabels();
        }
        finally
        {
            _loadingRule = false;
            UpdateRuleOptionVisibility();
        }
    }

    private void LoadSelectedStripIntoUi()
    {
        _loadingStrip = true;

        try
        {
            StripEditorPanel.IsEnabled = StripsList.SelectedItem is LedStripConfig;

            if (StripsList.SelectedItem is not LedStripConfig strip)
            {
                return;
            }

            StripNameBox.Text = strip.Name;
            StripPinBox.Text = strip.Pin.ToString();
            StripLedCountBox.Text = strip.LedCount.ToString();
        }
        finally
        {
            _loadingStrip = false;
        }
    }

    private void SaveGlobalSettingsFromFields()
    {
        if (_loadingUi)
        {
            return;
        }

        _config.TwitchClientId = ClientIdBox.Text.Trim();
        _config.TwitchClientSecret = ClientSecretBox.Text.Trim();
        _config.SerialPort = ParsePort(PortComboBox.SelectedValue as string ?? PortComboBox.Text);
        _config.BaudRate = ParseInt(BaudRateBox.Text, 115200, 300, 921600);
        _config.AutoConnectTwitch = AutoTwitchCheck.IsChecked == true;
        _config.AutoConnectArduino = AutoArduinoCheck.IsChecked == true;
        _config.StartHidden = StartHiddenCheck.IsChecked == true;
        _config.ThemeMode = NormalizeThemeMode(ThemeModeBox.SelectedValue as string ?? _config.ThemeMode);
        _config.DarkMode = ResolveDarkMode(_config.ThemeMode);
        _config.CloseToTray = CloseToTrayCheck.IsChecked == true;
        _config.AlertVolumePercent = (int)Math.Round(AlertVolumeSlider.Value);
        _config.MaxQueuedSameRuleAlerts = ParseInt(MaxQueuedSameRuleAlertsBox.Text, 1, 0, 100);
        _config.SameRuleQueueCooldownMs = ParseInt(SameRuleQueueCooldownBox.Text, 0, 0, 600000);
        _config.MaxQueuedDifferentRuleAlerts = ParseInt(MaxQueuedDifferentRuleAlertsBox.Text, 3, 0, 100);
        _config.DifferentRuleQueueCooldownMs = ParseInt(DifferentRuleQueueCooldownBox.Text, 0, 0, 600000);
        _config.Alexa.Enabled = AlexaEnabledCheck.IsChecked == true;
        _config.Alexa.RelayUrl = AlexaRelayUrlBox.Text.Trim();
        _config.Alexa.AuthToken = AlexaAuthTokenBox.Text.Trim();
    }

    private void SaveCurrentRuleFromFields()
    {
        if (_loadingRule || RulesList.SelectedItem is not EventRule rule)
        {
            return;
        }

        rule.IsEnabled = RuleEnabledCheck.IsChecked == true;
        rule.Name = RuleNameBox.Text.Trim();
        rule.EventKind = EventKindBox.SelectedValue is TwitchEventKind kind ? kind : TwitchEventKind.Follow;
        rule.CustomRewardTitle = RewardTitleBox.Text.Trim();
        rule.ChatCommand = ChatCommandBox.Text.Trim();
        rule.MinimumBits = ParseInt(MinimumBitsBox.Text, 1, 1, 1_000_000);
        rule.SendChatMessage = ChatMessageCheck.IsChecked == true;
        rule.ChatMessageTemplate = ChatMessageBox.Text.Trim();
        rule.SendAlexaEvent = AlexaEventCheck.IsChecked == true;
        rule.UseLights = UseLightsCheck.IsChecked == true;
        rule.PlayAudio = PlayAudioCheck.IsChecked == true;
        rule.AudioPath = AudioPathBox.Text.Trim();
        rule.Pattern = PatternBox.SelectedValue is LightPattern pattern ? pattern : LightPattern.Pulse;
        rule.TargetPins = string.Join(", ", LightCommand.ParsePins(TargetPinsBox.Text));
        rule.PrimaryColor = LightCommand.NormalizeColor(PrimaryColorBox.Text);
        rule.SecondaryColor = LightCommand.NormalizeColor(SecondaryColorBox.Text);
        rule.TertiaryColor = LightCommand.NormalizeColor(TertiaryColorBox.Text);
        rule.Brightness = (int)Math.Round(BrightnessSlider.Value);
        rule.DurationMs = (int)Math.Round(DurationSlider.Value);
        rule.CycleMs = (int)Math.Round(CycleSlider.Value);
        rule.StepMs = (int)Math.Round(StepSlider.Value);

        UpdateColorButtons();
        UpdateSliderLabels();
        UpdateRuleOptionVisibility();
        RulesList.Items.Refresh();
    }

    private void SaveBackgroundFromFields()
    {
        _config.BackgroundEnabled = BackgroundEnabledCheck.IsChecked == true;
        _config.BackgroundAlexaEnabled = BackgroundAlexaEnabledCheck.IsChecked == true;
        _config.BackgroundAlexaTurnOffAfterEvent = BackgroundAlexaTurnOffAfterEventCheck.IsChecked == true;
        _config.BackgroundAlexaOnEventName = NormalizeEventName(BackgroundAlexaOnEventBox.Text, "luz_encendida");
        _config.BackgroundAlexaOffEventName = NormalizeEventName(BackgroundAlexaOffEventBox.Text, "luz_apagada");
        _config.BackgroundTargetPins = string.Join(", ", LightCommand.ParsePins(BackgroundPinsBox.Text));
        _config.BackgroundPattern = BackgroundPatternBox.SelectedValue is LightPattern pattern ? pattern : LightPattern.Solid;
        _config.BackgroundPrimaryColor = LightCommand.NormalizeColor(BackgroundPrimaryColorBox.Text);
        _config.BackgroundSecondaryColor = LightCommand.NormalizeColor(BackgroundSecondaryColorBox.Text);
        _config.BackgroundTertiaryColor = LightCommand.NormalizeColor(BackgroundTertiaryColorBox.Text);
        _config.BackgroundBrightness = (int)Math.Round(BackgroundBrightnessSlider.Value);
        _config.BackgroundCycleMs = (int)Math.Round(BackgroundCycleSlider.Value);
        _config.BackgroundStepMs = (int)Math.Round(BackgroundStepSlider.Value);

        UpdateColorButtons();
        UpdateSliderLabels();
        UpdateBackgroundOptionVisibility();
        UpdateAlexaStatusText();
    }

    private void SaveCurrentStripFromFields()
    {
        if (_loadingStrip || StripsList.SelectedItem is not LedStripConfig strip)
        {
            return;
        }

        strip.Name = string.IsNullOrWhiteSpace(StripNameBox.Text)
            ? "Tira LED"
            : StripNameBox.Text.Trim();
        strip.Pin = ParseInt(StripPinBox.Text, 6, 0, 53);
        strip.LedCount = ParseInt(StripLedCountBox.Text, 30, 1, 600);

        StripsList.Items.Refresh();
        RulesList.Items.Refresh();
    }

    private void UpdateRuleOptionVisibility()
    {
        var kind = EventKindBox.SelectedValue is TwitchEventKind eventKind
            ? eventKind
            : TwitchEventKind.Follow;
        var useLights = UseLightsCheck.IsChecked == true;
        var playAudio = PlayAudioCheck.IsChecked == true;
        var sendChat = ChatMessageCheck.IsChecked == true;
        var alexaAvailable = _config.Alexa.IsConfigured;
        var sendAlexa = AlexaEventCheck.IsChecked == true;
        var pattern = PatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Pulse;

        SetVisible(kind == TwitchEventKind.ChannelPointRedemption, RewardTitleLabel, RewardTitleBox);
        SetVisible(kind == TwitchEventKind.ChatCommand, ChatCommandLabel, ChatCommandBox);
        SetVisible(kind == TwitchEventKind.Cheer, MinimumBitsLabel, MinimumBitsBox);
        SetVisible(playAudio, AudioLabel, AudioPanel);
        SetVisible(sendChat, ChatMessageLabel, ChatMessageBox);
        SetVisible(alexaAvailable, AlexaEventCheck);
        SetVisible(alexaAvailable && sendAlexa, AlexaRuleHintText);

        SetVisible(useLights, LightOptionsSeparator, TargetPinsLabel, TargetPinsBox, PatternGrid);
        SetVisible(useLights && UsesPrimaryColor(pattern), PrimaryColorPanel);
        SetVisible(useLights && UsesSecondaryColor(pattern), SecondaryColorLabel, SecondaryColorPanel);
        SetVisible(useLights && UsesTertiaryColor(pattern), TertiaryColorLabel, TertiaryColorPanel);
        SetVisible(useLights && UsesBrightness(pattern), BrightnessGrid, BrightnessSlider);
        SetVisible(useLights && !playAudio, DurationGrid, DurationSlider);
        SetVisible(useLights && UsesCycle(pattern), CycleGrid, CycleSlider);
        SetVisible(useLights && UsesStep(pattern), StepGrid, StepSlider);
    }

    private void UpdateBackgroundOptionVisibility()
    {
        var enabled = BackgroundEnabledCheck.IsChecked == true;
        var alexaEnabled = BackgroundAlexaEnabledCheck.IsChecked == true;
        var alexaTurnOffAfterEvent = BackgroundAlexaTurnOffAfterEventCheck.IsChecked == true;
        var alexaAvailable = _config.Alexa.IsConfigured;
        var pattern = BackgroundPatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Solid;

        SetVisible(alexaAvailable, BackgroundAlexaEnabledCheck, BackgroundAlexaTurnOffAfterEventCheck, StopAlexaBackgroundButton);
        SetVisible(!alexaAvailable, AlexaBackgroundUnavailableText);
        SetVisible(alexaAvailable && (alexaEnabled || alexaTurnOffAfterEvent), BackgroundAlexaEventsGrid, ApplyAlexaBackgroundButton);
        SetVisible(enabled, BackgroundPinsLabel, BackgroundPinsBox, BackgroundPatternGrid, ApplyArduinoBackgroundButton);
        SetVisible(enabled && UsesBrightness(pattern), BackgroundBrightnessPanel);
        SetVisible(enabled && UsesPrimaryColor(pattern), BackgroundPrimaryColorLabel, BackgroundPrimaryColorPanel);
        SetVisible(enabled && UsesSecondaryColor(pattern), BackgroundSecondaryColorLabel, BackgroundSecondaryColorPanel);
        SetVisible(enabled && UsesTertiaryColor(pattern), BackgroundTertiaryColorLabel, BackgroundTertiaryColorPanel);
        SetVisible(enabled && UsesCycle(pattern), BackgroundCycleGrid, BackgroundCycleSlider);
        SetVisible(enabled && UsesStep(pattern), BackgroundStepGrid, BackgroundStepSlider);
    }

    private void ApplyBackgroundOutputMode()
    {
        if (_initializingComponent)
        {
            return;
        }

        var showArduino = _backgroundOutputMode == BackgroundOutputMode.Arduino;
        SetVisible(showArduino, StripActionsPanel, StripsListLabel, StripsList, ArduinoBackgroundPanel);
        SetVisible(!showArduino, AlexaBackgroundPanel);

        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        ApplyOutputButtonTheme(ArduinoOutputButton, showArduino, palette);
        ApplyOutputButtonTheme(AlexaOutputButton, !showArduino, palette);
        UpdateBackgroundOptionVisibility();
    }

    private static void ApplyOutputButtonTheme(System.Windows.Controls.Button button, bool selected, ThemePalette palette)
    {
        button.Background = selected ? palette.NavSelected : palette.Button;
        button.Foreground = selected ? System.Windows.Media.Brushes.White : palette.Text;
        button.BorderBrush = selected ? palette.NavSelected : palette.Border;
    }

    private static void SetVisible(bool isVisible, params UIElement[] elements)
    {
        var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        foreach (var element in elements)
        {
            element.Visibility = visibility;
        }
    }

    private static bool UsesPrimaryColor(LightPattern pattern)
    {
        return pattern != LightPattern.Rainbow;
    }

    private static bool UsesSecondaryColor(LightPattern pattern)
    {
        return pattern is LightPattern.Pulse
            or LightPattern.Chase
            or LightPattern.Theater
            or LightPattern.Sparkle
            or LightPattern.Rave;
    }

    private static bool UsesTertiaryColor(LightPattern pattern)
    {
        return pattern is LightPattern.Chase
            or LightPattern.Theater
            or LightPattern.Sparkle
            or LightPattern.Rave;
    }

    private static bool UsesBrightness(LightPattern pattern)
    {
        return true;
    }

    private static bool UsesCycle(LightPattern pattern)
    {
        return pattern != LightPattern.Solid;
    }

    private static bool UsesStep(LightPattern pattern)
    {
        return pattern is LightPattern.Sparkle or LightPattern.Rave;
    }

    private void UpdateStatusText()
    {
        var channelName = _config.Channel.IsReady
            ? FirstNonEmpty(_config.Channel.DisplayName, _config.Channel.Login, "Canal Twitch")
            : "Sin Twitch";
        var login = _config.Channel.IsReady && !string.IsNullOrWhiteSpace(_config.Channel.Login)
            ? $"@{_config.Channel.Login}"
            : "Sin login";

        ChannelNameText.Text = channelName;
        ChannelLoginText.Text = login;
        TwitchConnectionText.Text = _eventSubClient.IsRunning
            ? "Eventos conectados"
            : _config.Token.HasToken
                ? "Sesion autorizada"
                : "Sin conectar";
        TwitchStatusText.Text = BuildTwitchStatusText();
        UpdateTwitchLiveIndicator();
        UpdateChannelAvatar();

        var totalLeds = _config.LedStrips.Sum(strip => strip.LedCount);
        var activeBackground = _config.BackgroundEnabled
            ? $"{DisplayNames.For(_config.BackgroundPattern)} de fondo"
            : "Fondo apagado";
        ArduinoConnectionText.Text = _lightController.HasOpenPort
            ? $"Conectado en {_lightController.CurrentPort}"
            : "Sin conectar";
        ArduinoStatusText.Text = _lightController.HasOpenPort
            ? $"{_config.BaudRate} baudios. {_config.LedStrips.Count} tiras, {totalLeds} LEDs. {activeBackground}."
            : $"Puerto: {FirstNonEmpty(_config.SerialPort, "sin COM")}. {_config.LedStrips.Count} tiras, {totalLeds} LEDs.";

        SetButtonIcon(
            TwitchButton,
            _eventSubClient.IsRunning ? "Desconectar Twitch" : "Conectar Twitch",
            _eventSubClient.IsRunning ? "Power" : "Plug");
    }

    private void UpdateAlexaStatusText()
    {
        var status = _config.Alexa.IsConfigured
            ? "Alexa lista. Las reglas pueden enviar eventos a la Skill/relay."
            : _config.Alexa.Enabled
                ? "Alexa activa, falta configurar una URL valida de Skill/relay."
                : "Alexa desactivada. Las reglas no mostraran acciones de Alexa.";

        AlexaStatusText.Text = status;
        AlexaConnectionText.Text = _config.Alexa.IsConfigured
            ? "Relay conectado"
            : _config.Alexa.Enabled
                ? "Configuracion incompleta"
                : "Sin conectar";
        AlexaSidebarStatusText.Text = _config.Alexa.IsConfigured
            ? BuildAlexaSidebarStatusText()
            : status;
    }

    private string BuildAlexaSidebarStatusText()
    {
        var background = _config.BackgroundAlexaEnabled
            ? $"Fondo: {_config.BackgroundAlexaOnEventName}"
            : "Fondo sin mantener";

        var endBehavior = _config.BackgroundAlexaTurnOffAfterEvent
            ? $"Al finalizar: {_config.BackgroundAlexaOffEventName}"
            : "Al finalizar: conserva estado";

        return $"{background}. {endBehavior}.";
    }

    private void UpdateTwitchLiveIndicator()
    {
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        if (_streamStatus is { IsLive: true })
        {
            var liveBrush = FrozenBrushFrom("#FF2D55");
            TwitchLiveDot.Fill = liveBrush;
            TwitchLiveDot.Stroke = liveBrush;
            TwitchLiveStateText.Text = "En directo";
            TwitchLiveStateText.Foreground = liveBrush;
            return;
        }

        TwitchLiveDot.Fill = System.Windows.Media.Brushes.Transparent;
        TwitchLiveDot.Stroke = palette.SidebarText;
        TwitchLiveStateText.Text = "No esta en directo";
        TwitchLiveStateText.Foreground = palette.SidebarText;
    }

    private string BuildTwitchStatusText()
    {
        if (_streamStatus is { IsLive: true } live)
        {
            var game = string.IsNullOrWhiteSpace(live.GameName)
                ? ""
                : $" en {live.GameName}";
            return $"En directo{game}. {live.ViewerCount} espectadores.";
        }

        if (_streamStatus is { IsLive: false })
        {
            return "Canal sin directo activo.";
        }

        return _eventSubClient.IsRunning
            ? "Escuchando eventos. Directo sin consultar."
            : "Listo para conectar eventos.";
    }

    private void UpdateChannelAvatar()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_config.Channel.ProfileImageUrl))
            {
                ChannelAvatarImage.Source = new BitmapImage(new Uri(_config.Channel.ProfileImageUrl, UriKind.Absolute));
                return;
            }
        }
        catch
        {
            // Use the bundled app icon when Twitch has no image available.
        }

        ChannelAvatarImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/AppIcon.png", UriKind.Absolute));
    }

    private void ApplyWindowChromeColor()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            var captionColor = AppCaptionColor;
            var borderColor = AppCaptionColor;
            var textColor = AppCaptionTextColor;
            var size = Marshal.SizeOf<int>();
            _ = DwmSetWindowAttribute(hwnd, DwmWindowAttributeCaptionColor, ref captionColor, size);
            _ = DwmSetWindowAttribute(hwnd, DwmWindowAttributeBorderColor, ref borderColor, size);
            _ = DwmSetWindowAttribute(hwnd, DwmWindowAttributeTextColor, ref textColor, size);
        }
        catch
        {
            // Older Windows builds ignore custom title bar colors.
        }
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }

    private static string FormatNameList(IReadOnlyList<string> names)
    {
        var visibleNames = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Take(5)
            .ToArray();

        var text = visibleNames.Length == 0
            ? "sin nombre"
            : string.Join(", ", visibleNames);
        var remaining = names.Count - visibleNames.Length;
        return remaining > 0 ? $"{text} y {remaining} mas" : text;
    }

    private static string NormalizeThemeMode(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "light" => "Light",
            "dark" => "Dark",
            _ => "System"
        };
    }

    private static bool ResolveDarkMode(string? themeMode)
    {
        return NormalizeThemeMode(themeMode) switch
        {
            "Dark" => true,
            "Light" => false,
            _ => IsWindowsAppsDarkMode()
        };
    }

    private static bool IsWindowsAppsDarkMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeEventName(string text, string fallback)
    {
        return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
    }

    private void UpdateSliderLabels()
    {
        BrightnessValueText.Text = ((int)Math.Round(BrightnessSlider.Value)).ToString();
        DurationValueText.Text = $"{(int)Math.Round(DurationSlider.Value)} ms";
        CycleValueText.Text = $"{(int)Math.Round(CycleSlider.Value)} ms";
        StepValueText.Text = $"{(int)Math.Round(StepSlider.Value)} ms";
        BackgroundBrightnessValueText.Text = ((int)Math.Round(BackgroundBrightnessSlider.Value)).ToString();
        BackgroundCycleValueText.Text = $"{(int)Math.Round(BackgroundCycleSlider.Value)} ms";
        BackgroundStepValueText.Text = $"{(int)Math.Round(BackgroundStepSlider.Value)} ms";
        AlertVolumeValueText.Text = $"{(int)Math.Round(AlertVolumeSlider.Value)}%";
    }

    private void UpdateColorButtons()
    {
        PrimaryColorButton.Background = ToBrush(PrimaryColorBox.Text);
        SecondaryColorButton.Background = ToBrush(SecondaryColorBox.Text);
        TertiaryColorButton.Background = ToBrush(TertiaryColorBox.Text);
        BackgroundPrimaryColorButton.Background = ToBrush(BackgroundPrimaryColorBox.Text);
        BackgroundSecondaryColorButton.Background = ToBrush(BackgroundSecondaryColorBox.Text);
        BackgroundTertiaryColorButton.Background = ToBrush(BackgroundTertiaryColorBox.Text);
    }

    private void UpdateSensitiveFieldVisibility()
    {
        if (_initializingComponent)
        {
            return;
        }

        UpdateSensitiveField(ClientIdBox, ClientIdMaskText, ClientIdRevealButton, _showClientId);
        UpdateSensitiveField(ClientSecretBox, ClientSecretMaskText, ClientSecretRevealButton, _showClientSecret);
        UpdateSensitiveField(AlexaRelayUrlBox, AlexaRelayUrlMaskText, AlexaRelayUrlRevealButton, _showAlexaRelayUrl);
        UpdateSensitiveField(AlexaAuthTokenBox, AlexaAuthTokenMaskText, AlexaAuthTokenRevealButton, _showAlexaAuthToken);
    }

    private static void UpdateSensitiveField(
        System.Windows.Controls.TextBox textBox,
        TextBlock maskText,
        System.Windows.Controls.Button revealButton,
        bool isVisible)
    {
        var shouldMask = !isVisible && !string.IsNullOrWhiteSpace(textBox.Text);
        textBox.IsHitTestVisible = !shouldMask;
        maskText.Visibility = shouldMask ? Visibility.Visible : Visibility.Collapsed;
        maskText.Text = shouldMask ? BuildMask(textBox.Text) : "";
        revealButton.Content = isVisible ? "Ocultar" : "Ver";
    }

    private static string BuildMask(string value)
    {
        var length = Math.Clamp(value.Trim().Length, 8, 20);
        return new string('*', length);
    }

    private void ApplyTheme()
    {
        _config.DarkMode = ResolveDarkMode(_config.ThemeMode);
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        Background = palette.Window;
        Resources["ThemeWindowBrush"] = palette.Window;
        Resources["ThemeSidebarBrush"] = palette.Sidebar;
        Resources["ThemeSurfaceBrush"] = palette.Surface;
        Resources["ThemeButtonBrush"] = palette.Button;
        Resources["ThemeTextBrush"] = palette.Text;
        Resources["ThemeMutedTextBrush"] = palette.MutedText;
        Resources["ThemeSidebarTextBrush"] = palette.SidebarText;
        Resources["ThemeSidebarMutedTextBrush"] = palette.SidebarMutedText;
        Resources["ThemeInputBrush"] = palette.Input;
        Resources["ThemeBorderBrush"] = palette.Border;
        Resources["ThemeSelectionBrush"] = palette.Accent;
        Resources["ThemeConsoleBrush"] = palette.Console;
        Resources["ThemeScrollThumbBrush"] = palette.Accent;
        Resources["ThemeScrollTrackBrush"] = palette.ScrollTrack;
        Resources[System.Windows.SystemColors.WindowBrushKey] = palette.Input;
        Resources[System.Windows.SystemColors.ControlBrushKey] = palette.Input;
        Resources[System.Windows.SystemColors.WindowTextBrushKey] = palette.Text;
        Resources[System.Windows.SystemColors.ControlTextBrushKey] = palette.Text;
        Resources[System.Windows.SystemColors.HighlightBrushKey] = palette.Accent;
        Resources[System.Windows.SystemColors.HighlightTextBrushKey] = System.Windows.Media.Brushes.White;
        Resources[System.Windows.SystemColors.InactiveSelectionHighlightBrushKey] = palette.Accent;
        Resources[System.Windows.SystemColors.InactiveSelectionHighlightTextBrushKey] = System.Windows.Media.Brushes.White;
        ApplyWindowChromeColor();
        UpdateNavigationButtons();
        ApplyBackgroundOutputMode();
        ApplyThemeToElement(this, palette);
        ApplyBackgroundOutputMode();
        UpdateTwitchLiveIndicator();
        UpdateColorButtons();
    }

    private void ApplyThemeToElement(DependencyObject element, ThemePalette palette)
    {
        var skipChildren = false;

        switch (element)
        {
            case Border border when border.TemplatedParent is not null:
                break;
            case Border border:
                border.BorderBrush = palette.Border;
                if (IsSidebarBorder(border))
                {
                    border.Background = palette.Sidebar;
                    break;
                }

                if (IsConsoleBorder(border))
                {
                    border.Background = palette.Console;
                    break;
                }

                if (IsInsideNamedElement(border, "SidebarChrome"))
                {
                    border.Background = palette.SidebarCard;
                    border.BorderBrush = palette.SidebarCardBorder;
                    break;
                }

                border.Background = palette.Surface;
                break;
            case TextBlock textBlock when textBlock.DataContext is ActivityLogEntry:
                break;
            case TextBlock textBlock:
                if (IsInsideNamedElement(textBlock, "SidebarChrome"))
                {
                    textBlock.Foreground = textBlock.FontSize <= 12 || textBlock.Name.Contains("Status", StringComparison.OrdinalIgnoreCase)
                        ? palette.SidebarMutedText
                        : palette.SidebarText;
                    break;
                }

                if (IsInsideNamedElement(textBlock, "MiniConsolePanel"))
                {
                    textBlock.Foreground = textBlock.FontSize <= 12
                        ? palette.ConsoleMutedText
                        : System.Windows.Media.Brushes.White;
                    break;
                }

                textBlock.Foreground = textBlock.FontSize <= 12 || textBlock.Name.Contains("Status", StringComparison.OrdinalIgnoreCase)
                    ? palette.MutedText
                    : palette.Text;
                break;
            case System.Windows.Controls.TextBox textBox:
                textBox.Background = palette.Input;
                textBox.Foreground = palette.Text;
                textBox.BorderBrush = palette.Border;
                textBox.CaretBrush = palette.Text;
                break;
            case System.Windows.Controls.ComboBox comboBox:
                comboBox.Background = palette.Input;
                comboBox.Foreground = palette.Text;
                comboBox.BorderBrush = palette.Border;
                comboBox.Resources[System.Windows.SystemColors.WindowBrushKey] = palette.Input;
                comboBox.Resources[System.Windows.SystemColors.ControlBrushKey] = palette.Input;
                comboBox.Resources[System.Windows.SystemColors.WindowTextBrushKey] = palette.Text;
                comboBox.Resources[System.Windows.SystemColors.ControlTextBrushKey] = palette.Text;
                comboBox.Resources[System.Windows.SystemColors.HighlightBrushKey] = palette.Accent;
                comboBox.Resources[System.Windows.SystemColors.HighlightTextBrushKey] = System.Windows.Media.Brushes.White;
                break;
            case System.Windows.Controls.ListBox listBox:
                if (IsInsideNamedElement(listBox, "MiniConsolePanel"))
                {
                    listBox.Background = System.Windows.Media.Brushes.Transparent;
                    listBox.Foreground = System.Windows.Media.Brushes.White;
                    listBox.BorderBrush = System.Windows.Media.Brushes.Transparent;
                    break;
                }

                listBox.Background = palette.Input;
                listBox.Foreground = palette.Text;
                listBox.BorderBrush = palette.Border;
                break;
            case System.Windows.Controls.TabControl tabControl:
                tabControl.Background = System.Windows.Media.Brushes.Transparent;
                tabControl.BorderBrush = palette.Border;
                tabControl.Foreground = palette.Text;
                break;
            case TabItem tabItem:
                tabItem.Background = palette.Surface;
                tabItem.Foreground = palette.Text;
                tabItem.BorderBrush = palette.Border;
                break;
            case System.Windows.Controls.CheckBox checkBox:
                checkBox.Foreground = palette.Text;
                checkBox.Background = palette.Input;
                checkBox.BorderBrush = palette.MutedText;
                skipChildren = true;
                break;
            case Slider slider:
                slider.Foreground = palette.Accent;
                break;
            case System.Windows.Controls.Button button when IsColorButton(button):
                button.BorderBrush = palette.Border;
                skipChildren = true;
                break;
            case System.Windows.Controls.Button button:
                ApplyButtonTheme(button, palette);
                skipChildren = true;
                break;
        }

        if (skipChildren)
        {
            return;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            ApplyThemeToElement(VisualTreeHelper.GetChild(element, i), palette);
        }
    }

    private void ApplyButtonTheme(System.Windows.Controls.Button button, ThemePalette palette)
    {
        if (ReferenceEquals(button.Style, Resources["NavButton"]))
        {
            ApplyNavigationButtonTheme(button, palette);
            return;
        }

        if (ReferenceEquals(button.Style, Resources["PrimaryButton"]))
        {
            button.Background = palette.Accent;
            button.Foreground = System.Windows.Media.Brushes.White;
            button.BorderBrush = palette.Accent;
            return;
        }

        if (ReferenceEquals(button.Style, Resources["DangerButton"]))
        {
            button.Background = palette.DangerSurface;
            button.Foreground = palette.DangerText;
            button.BorderBrush = palette.DangerBorder;
            return;
        }

        button.Background = palette.Button;
        button.Foreground = palette.Text;
        button.BorderBrush = palette.Border;
    }

    private void UpdateNavigationButtons()
    {
        if (_initializingComponent)
        {
            return;
        }

        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        foreach (var button in new[] { NavSettingsButton, NavRulesButton, NavStripsButton, NavPreferencesButton, NavActivityButton })
        {
            ApplyNavigationButtonTheme(button, palette);
        }
    }

    private void ApplyNavigationButtonTheme(System.Windows.Controls.Button button, ThemePalette palette)
    {
        var isSelected = int.TryParse(button.Tag?.ToString(), out var index)
            && index == MainTabs.SelectedIndex;

        button.Background = isSelected
            ? palette.NavSelected
            : System.Windows.Media.Brushes.Transparent;
        button.Foreground = isSelected
            ? System.Windows.Media.Brushes.White
            : palette.SidebarMutedText;
        button.BorderBrush = System.Windows.Media.Brushes.Transparent;
    }

    private static bool IsColorButton(System.Windows.Controls.Button button)
    {
        return !string.IsNullOrWhiteSpace(button.Name)
            && button.Name.EndsWith("ColorButton", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSidebarBorder(Border border)
    {
        return string.Equals(border.Name, "SidebarChrome", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsConsoleBorder(Border border)
    {
        return string.Equals(border.Name, "MiniConsolePanel", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInsideNamedElement(DependencyObject element, string name)
    {
        for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is FrameworkElement frameworkElement
                && string.Equals(frameworkElement.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static SolidColorBrush ToBrush(string color)
    {
        try
        {
            return new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(LightCommand.NormalizeColor(color)));
        }
        catch
        {
            return new SolidColorBrush(Colors.White);
        }
    }

    private void PickColor(System.Windows.Controls.TextBox target)
    {
        using var dialog = new Forms.ColorDialog
        {
            FullOpen = true
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK)
        {
            return;
        }

        target.Text = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
    }

    private void CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Abrir", null, (_, _) => ShowFromTray());
        menu.Items.Add("Salir", null, async (_, _) => await ExitApplicationAsync());

        _trayIcon = LoadAppIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = _trayIcon,
            Text = "Neo Twitch",
            Visible = true,
            ContextMenuStrip = menu
        };

        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();
    }

    private static DrawingIcon LoadAppIcon()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(Environment.ProcessPath) && File.Exists(Environment.ProcessPath))
            {
                var icon = DrawingIcon.ExtractAssociatedIcon(Environment.ProcessPath);
                if (icon is not null)
                {
                    return icon;
                }
            }
        }
        catch
        {
            // Try the bundled WPF resource below.
        }

        try
        {
            var resource = System.Windows.Application.GetResourceStream(new Uri("Assets/AppIcon.ico", UriKind.Relative));
            if (resource?.Stream is not null)
            {
                using var stream = resource.Stream;
                using var icon = new DrawingIcon(stream);
                return (DrawingIcon)icon.Clone();
            }
        }
        catch
        {
            // Fall back to a generic app icon only if the bundled icon cannot be loaded.
        }

        return (DrawingIcon)SystemIcons.Application.Clone();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private async Task ExitApplicationAsync()
    {
        _isExiting = true;
        SaveGlobalSettingsFromFields();
        SaveCurrentRuleFromFields();
        SaveCurrentStripFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        _backgroundApplyDebounce?.Cancel();
        _backgroundApplyDebounce?.Dispose();
        _twitchSubscriptionRefreshDebounce?.Cancel();
        _twitchSubscriptionRefreshDebounce?.Dispose();
        await _eventSubClient.StopAsync();
        _chatService.Dispose();
        _lightController.Dispose();
        DisposeTrayIcon();
        Close();
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_isExiting)
        {
            SaveGlobalSettingsFromFields();
            SaveCurrentRuleFromFields();
            SaveCurrentStripFromFields();
            SaveBackgroundFromFields();
            SaveConfig();

            if (_config.CloseToTray)
            {
                e.Cancel = true;
                _twitchSubscriptionRefreshDebounce?.Cancel();
                Hide();
                ShowTrayBackgroundNotice();
                AddLog("Ventana oculta en segundo plano.");
                return;
            }

            _isExiting = true;
        }

        await _eventSubClient.StopAsync();
        _chatService.Dispose();
        _lightController.Dispose();
        DisposeTrayIcon();
    }

    private void ShowTrayBackgroundNotice()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        try
        {
            _notifyIcon.BalloonTipTitle = "Neo Twitch sigue activo";
            _notifyIcon.BalloonTipText = "La app quedo en segundo plano. Abrela desde el icono de la bandeja cuando la necesites.";
            _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Info;
            _notifyIcon.ShowBalloonTip(_hasShownTrayNotice ? 2500 : 4000);
            _hasShownTrayNotice = true;
        }
        catch
        {
            // Windows can suppress tray notifications; the app still remains available in the tray.
        }
    }

    private void DisposeTrayIcon()
    {
        _notifyIcon?.Dispose();
        _notifyIcon = null;
        _trayIcon?.Dispose();
        _trayIcon = null;
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
    }

    private void SaveConfig()
    {
        try
        {
            _settingsStore.Save(_config);
        }
        catch (Exception ex)
        {
            AddLog($"No pude guardar la configuracion: {ex.Message}");
        }
    }

    private void AddLog(string message)
    {
        AddLog(message, ClassifyLogMessage(message));
    }

    private void AddLog(string message, ActivityLogKind kind)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _activity.Insert(0, new ActivityLogEntry(message, kind));

            while (_activity.Count > 250)
            {
                _activity.RemoveAt(_activity.Count - 1);
            }
        });
    }

    private static ActivityLogKind ClassifyLogMessage(string message)
    {
        var text = message.ToLowerInvariant();

        if (text.Contains("error", StringComparison.Ordinal)
            || text.Contains("fallo", StringComparison.Ordinal)
            || text.Contains("no pude", StringComparison.Ordinal)
            || text.Contains("no puedo", StringComparison.Ordinal)
            || text.Contains("no hay", StringComparison.Ordinal)
            || text.Contains("no encontre", StringComparison.Ordinal))
        {
            return ActivityLogKind.Important;
        }

        if (text.StartsWith("twitch", StringComparison.Ordinal)
            || text.StartsWith("alexa", StringComparison.Ordinal)
            || text.StartsWith("chat", StringComparison.Ordinal)
            || text.Contains("autorizado", StringComparison.Ordinal)
            || text.Contains("escuchando eventos", StringComparison.Ordinal))
        {
            return text.StartsWith("alexa", StringComparison.Ordinal)
                ? ActivityLogKind.Alexa
                : ActivityLogKind.Twitch;
        }

        if (text.Contains("siguio", StringComparison.Ordinal)
            || text.Contains("suscribio", StringComparison.Ordinal)
            || text.Contains("raid", StringComparison.Ordinal)
            || text.Contains("bits", StringComparison.Ordinal)
            || text.Contains("canjeo", StringComparison.Ordinal)
            || text.StartsWith("prueba de", StringComparison.Ordinal))
        {
            return ActivityLogKind.Event;
        }

        return ActivityLogKind.Info;
    }

    private static string ParsePort(string text)
    {
        var ports = text.Split([',', ';', ' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => value.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            .Select(value => value.ToUpperInvariant())
            .ToArray();

        return ChoosePreferredPort(ports);
    }

    private static string ChoosePreferredPort(IReadOnlyList<string> ports)
    {
        if (ports.Count == 0)
        {
            return "";
        }

        return ports.FirstOrDefault(port => !string.Equals(port, "COM1", StringComparison.OrdinalIgnoreCase))
            ?? ports[0].ToUpperInvariant();
    }

    private static string ChoosePreferredPort(IReadOnlyList<SerialPortInfo> ports)
    {
        if (ports.Count == 0)
        {
            return "";
        }

        return ports.FirstOrDefault(port => port.IsLikelyArduino)?.PortName
            ?? ports.FirstOrDefault(port => !string.Equals(port.PortName, "COM1", StringComparison.OrdinalIgnoreCase))?.PortName
            ?? ports[0].PortName;
    }

    private static int ParseInt(string text, int fallback, int min, int max)
    {
        return int.TryParse(text, out var value)
            ? Math.Clamp(value, min, max)
            : fallback;
    }

    private sealed record UiOption<T>(string Label, T Value)
    {
        public override string ToString() => Label;
    }

    private sealed record DiagnosticResult(string Report, int WarningCount);

    private enum ActivityLogKind
    {
        Info,
        Twitch,
        Alexa,
        Event,
        Important
    }

    private enum BackgroundOutputMode
    {
        Arduino,
        Alexa
    }

    private sealed record QueuedAlertSlot(string Id, string RuleId, string RuleName, TwitchEventKind EventKind);

    private sealed class ActivityLogEntry
    {
        private static readonly SolidColorBrush InfoBrush = FrozenBrushFrom("#AFA4CC");
        private static readonly SolidColorBrush TwitchBrush = FrozenBrushFrom("#9146FF");
        private static readonly SolidColorBrush AlexaBrush = FrozenBrushFrom("#00A7CE");
        private static readonly SolidColorBrush EventBrush = FrozenBrushFrom("#00C7B7");
        private static readonly SolidColorBrush ImportantBrush = FrozenBrushFrom("#FFB020");

        public ActivityLogEntry(string message, ActivityLogKind kind)
        {
            Time = DateTime.Now.ToString("HH:mm:ss");
            Message = message;
            Category = kind switch
            {
                ActivityLogKind.Twitch => "TWITCH",
                ActivityLogKind.Alexa => "ALEXA",
                ActivityLogKind.Event => "EVENTO",
                ActivityLogKind.Important => "IMPORTANTE",
                _ => "SISTEMA"
            };
            AccentBrush = kind switch
            {
                ActivityLogKind.Twitch => TwitchBrush,
                ActivityLogKind.Alexa => AlexaBrush,
                ActivityLogKind.Event => EventBrush,
                ActivityLogKind.Important => ImportantBrush,
                _ => InfoBrush
            };
        }

        public string Time { get; }
        public string Message { get; }
        public string Category { get; }
        public SolidColorBrush AccentBrush { get; }
    }

    private static SolidColorBrush FrozenBrushFrom(string hex)
    {
        var brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int attributeValue,
        int attributeSize);

    private sealed record ThemePalette(
        SolidColorBrush Window,
        SolidColorBrush Sidebar,
        SolidColorBrush Surface,
        SolidColorBrush Input,
        SolidColorBrush Button,
        SolidColorBrush Border,
        SolidColorBrush Text,
        SolidColorBrush MutedText,
        SolidColorBrush SidebarText,
        SolidColorBrush SidebarMutedText,
        SolidColorBrush SidebarCard,
        SolidColorBrush SidebarCardBorder,
        SolidColorBrush Console,
        SolidColorBrush ConsoleMutedText,
        SolidColorBrush ScrollTrack,
        SolidColorBrush Accent,
        SolidColorBrush NavSelected,
        SolidColorBrush DangerSurface,
        SolidColorBrush DangerText,
        SolidColorBrush DangerBorder)
    {
        public static ThemePalette Light { get; } = new(
            BrushFrom("#F7FAFC"),
            BrushFrom("#FFFFFF"),
            BrushFrom("#FFFFFF"),
            BrushFrom("#F8FAFC"),
            BrushFrom("#EEF2F6"),
            BrushFrom("#E2E8F0"),
            BrushFrom("#0B1117"),
            BrushFrom("#475569"),
            BrushFrom("#0B1117"),
            BrushFrom("#64748B"),
            BrushFrom("#F8FAFC"),
            BrushFrom("#E2E8F0"),
            BrushFrom("#0B1117"),
            BrushFrom("#94A3B8"),
            BrushFrom("#E2E8F0"),
            BrushFrom("#14B8A6"),
            BrushFrom("#14B8A6"),
            BrushFrom("#FFF1F2"),
            BrushFrom("#B91C1C"),
            BrushFrom("#FDA4AF"));

        public static ThemePalette Dark { get; } = new(
            BrushFrom("#081117"),
            BrushFrom("#0F1822"),
            BrushFrom("#121A24"),
            BrushFrom("#0F1822"),
            BrushFrom("#162231"),
            BrushFrom("#233142"),
            BrushFrom("#E6EEF2"),
            BrushFrom("#A7B4BE"),
            BrushFrom("#E6EEF2"),
            BrushFrom("#A7B4BE"),
            BrushFrom("#162231"),
            BrushFrom("#233142"),
            BrushFrom("#050A0E"),
            BrushFrom("#64748B"),
            BrushFrom("#132330"),
            BrushFrom("#14B8A6"),
            BrushFrom("#092C2D"),
            BrushFrom("#3A1418"),
            BrushFrom("#FDA4AF"),
            BrushFrom("#7F1D1D"));

        private static SolidColorBrush BrushFrom(string hex)
        {
            var brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }
    }
}
