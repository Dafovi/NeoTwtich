using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Input;
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
    private const string WindowsRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string WindowsStartupValueName = "Neo Twitch";

    private readonly SettingsStore _settingsStore = new();
    private readonly AudioPlayerService _audioPlayer = new();
    private readonly SerialLightController _lightController = new();
    private readonly TwitchAuthService _authService = new();
    private readonly TwitchChatService _chatService = new();
    private readonly AlexaRelayService _alexaRelayService = new();
    private readonly VersionCheckService _versionCheckService = new();
    private readonly TwitchEventSubClient _eventSubClient;
    private readonly ObservableCollection<ActivityLogEntry> _activity = [];
    private readonly ObservableCollection<ActivityLogEntry> _dashboardActivity = [];
    private readonly ObservableCollection<AudioLibraryRow> _audioLibraryRows = [];
    private readonly ObservableCollection<AudioGroupRow> _audioGroupRows = [];
    private readonly ObservableCollection<RuleLedPreviewDot> _ruleLedPreviewDots = [];
    private readonly ObservableCollection<RuleLedPreviewDot> _backgroundLedPreviewDots = [];
    private readonly CollectionViewSource _activityViewSource = new();
    private readonly CollectionViewSource _rulesViewSource = new();
    private readonly DispatcherTimer _ruleLedPreviewTimer = new();
    private readonly DispatcherTimer _backgroundLedPreviewTimer = new();
    private readonly Random _previewRandom = new();
    private readonly Random _audioRandom = new();
    private readonly SemaphoreSlim _effectGate = new(1, 1);
    private readonly object _alertQueueSync = new();
    private readonly List<QueuedAlertSlot> _pendingAlertSlots = [];
    private readonly Dictionary<string, DateTimeOffset> _lastRuleStartTimes = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<SerialPortInfo> _availablePorts = [];
    private readonly HashSet<string> _activityEnabledFilters = new(StringComparer.OrdinalIgnoreCase)
    {
        "TWITCH",
        "ARDUINO",
        "ALEXA",
        "AUDIO",
        "EVENTO",
        "SISTEMA",
        "IMPORTANTE"
    };
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
    private readonly UiOption<string>[] _ruleCategoryOptions =
    [
        new("Todas las categorias", ""),
        new("Seguidores", nameof(TwitchEventKind.Follow)),
        new("Suscripciones", nameof(TwitchEventKind.Subscription)),
        new("Raids", nameof(TwitchEventKind.Raid)),
        new("Bits", nameof(TwitchEventKind.Cheer)),
        new("Comandos de chat", nameof(TwitchEventKind.ChatCommand)),
        new("Canjes de puntos", nameof(TwitchEventKind.ChannelPointRedemption))
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
    private bool _alexaRelayConnected;
    private bool _isTwitchConnecting;
    private bool _isArduinoConnecting;
    private bool _isAlexaConnecting;
    private bool? _lastAppliedStartWithWindows;
    private string _twitchConnectionError = "";
    private string _activitySearchText = "";
    private string _ruleSearchText = "";
    private string _ruleStatusFilter = "ALL";
    private string _ruleCategoryFilter = "";
    private string _audioSearchText = "";
    private string _audioFilter = "ALL";
    private string _audioGroupFilterId = "";
    private string _newAudioPath = "";
    private AudioSourceMode _ruleAudioMode = AudioSourceMode.Single;
    private bool _refreshingAudioLibrary;
    private string _audioGroupChoicesSignature = "";
    private string _audioAlertChoicesSignature = "";
    private CancellationTokenSource? _backgroundApplyDebounce;
    private CancellationTokenSource? _twitchSubscriptionRefreshDebounce;
    private CancellationTokenSource? _currentEffectCts;
    private string _eventSubscriptionSignature = "";
    private string _runningRuleId = "";
    private string _lastStartedRuleId = "";
    private DateTimeOffset _lastAlertStartAt = DateTimeOffset.MinValue;
    private bool _hasShownTrayNotice;
    private int _dashboardFollowersToday;
    private int _dashboardSubscriptionsToday;
    private int _dashboardBitsToday;
    private int _dashboardChatMessagesToday;
    private int _dashboardEventsToday;
    private int _ruleLedPreviewStep;
    private int _backgroundLedPreviewStep;
    private AudioPlayback? _currentPlayback;
    private AudioPlayback? _audioPreviewPlayback;
    private string _previewingAudioId = "";
    private TwitchStreamStatus? _streamStatus;
    private DrawingIcon? _trayIcon;
    private Forms.NotifyIcon? _notifyIcon;

    public ObservableCollection<AudioGroupChoice> AudioGroupChoices { get; } = [];

    public ObservableCollection<AudioAlertChoice> AudioAlertChoices { get; } = [];

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
            _activityViewSource.Source = _activity;
            _activityViewSource.Filter += ActivityViewSource_Filter;
            ActivityList.ItemsSource = _activityViewSource.View;
            DashboardActivityList.ItemsSource = _dashboardActivity;
            AudioLibraryList.ItemsSource = _audioLibraryRows;
            AudioGroupsList.ItemsSource = _audioGroupRows;
            for (var i = 0; i < 24; i++)
            {
                _ruleLedPreviewDots.Add(PreviewDot(ParsePreviewColor("#334155", "#334155"), 0.08));
                _backgroundLedPreviewDots.Add(PreviewDot(ParsePreviewColor("#334155", "#334155"), 0.08));
            }

            RuleLedPreviewList.ItemsSource = _ruleLedPreviewDots;
            _ruleLedPreviewTimer.Interval = TimeSpan.FromMilliseconds(120);
            _ruleLedPreviewTimer.Tick += (_, _) => UpdateRuleLedPreviewFrame();
            BackgroundLedPreviewList.ItemsSource = _backgroundLedPreviewDots;
            _backgroundLedPreviewTimer.Interval = TimeSpan.FromMilliseconds(120);
            _backgroundLedPreviewTimer.Tick += (_, _) => UpdateBackgroundLedPreviewFrame();
            _rulesViewSource.Source = _config.Rules;
            _rulesViewSource.Filter += RulesViewSource_Filter;
            RulesList.ItemsSource = _rulesViewSource.View;
            EventKindBox.ItemsSource = _eventOptions;
            EventKindBox.DisplayMemberPath = nameof(UiOption<TwitchEventKind>.Label);
            EventKindBox.SelectedValuePath = nameof(UiOption<TwitchEventKind>.Value);
            RuleCategoryFilterBox.ItemsSource = _ruleCategoryOptions;
            RuleCategoryFilterBox.DisplayMemberPath = nameof(UiOption<string>.Label);
            RuleCategoryFilterBox.SelectedValuePath = nameof(UiOption<string>.Value);
            RuleCategoryFilterBox.SelectedValue = "";
            RuleAudioAssetBox.ItemsSource = _config.AudioLibrary;
            RuleAudioAssetBox.DisplayMemberPath = nameof(AudioAssetConfig.DisplayName);
            RuleAudioAssetBox.SelectedValuePath = nameof(AudioAssetConfig.Id);
            RuleAudioGroupBox.ItemsSource = _config.AudioGroups;
            RuleAudioGroupBox.DisplayMemberPath = nameof(AudioGroupConfig.Name);
            RuleAudioGroupBox.SelectedValuePath = nameof(AudioGroupConfig.Id);
            NewAudioAlertBox.ItemsSource = AudioAlertChoices;
            NewAudioAlertBox.DisplayMemberPath = nameof(AudioAlertChoice.Name);
            NewAudioAlertBox.SelectedValuePath = nameof(AudioAlertChoice.Id);
            NewAudioGroupBox.ItemsSource = AudioGroupChoices;
            NewAudioGroupBox.DisplayMemberPath = nameof(AudioGroupChoice.Name);
            NewAudioGroupBox.SelectedValuePath = nameof(AudioGroupChoice.Id);
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
        NavSettingsButton.Content = CreateNavigationItem("Assets/Icons/nav_panel.png", "Panel");
        NavConnectionsButton.Content = CreateNavigationItem("Assets/Icons/nav_connections.png", "Conexiones");
        NavRulesButton.Content = CreateNavigationItem("Assets/Icons/nav_rules.png", "Alertas");
        NavStripsButton.Content = CreateNavigationItem("Assets/Icons/nav_lights.png", "Luces");
        NavAlexaButton.Content = CreateNavigationItem("Assets/Icons/nav_alexa.png", "Alexa");
        NavAudioButton.Content = CreateNavigationItem("Assets/Icons/nav_audio.png", "Audio");
        NavPreferencesButton.Content = CreateNavigationItem("Assets/Icons/nav_settings.png", "Configuracion");
        NavActivityButton.Content = CreateNavigationItem("Assets/Icons/nav_activity.png", "Actividad");
    }

    private static System.Windows.Shapes.Path CreateNavigationIcon(string data)
    {
        return CreateIconPath(data, 24, 2);
    }

    private static StackPanel CreateNavigationItem(string iconPath, string label)
    {
        var panel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(CreateTintedImageIcon(iconPath, 18));
        var text = new TextBlock
        {
            Text = label,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold
        };
        text.SetBinding(
            TextBlock.ForegroundProperty,
            new System.Windows.Data.Binding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(System.Windows.Controls.Button), 1)
            });
        panel.Children.Add(text);
        return panel;
    }

    private static Border CreateTintedImageIcon(string iconPath, double size)
    {
        var icon = new Border
        {
            Width = size,
            Height = size,
            Background = System.Windows.Media.Brushes.White,
            OpacityMask = new ImageBrush
            {
                ImageSource = LoadPackImage(iconPath),
                Stretch = Stretch.Uniform
            }
        };

        icon.SetBinding(
            Border.BackgroundProperty,
            new System.Windows.Data.Binding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(System.Windows.Controls.Button), 1)
            });

        return icon;
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
            ["Ir a actividad"] = "Activity",
            ["Nueva"] = "Plus",
            ["Nueva alerta"] = "Plus",
            ["Duplicar"] = "Copy",
            ["Eliminar"] = "Trash",
            ["Probar regla"] = "Play",
            ["Probar alerta"] = "Play",
            ["Parar prueba"] = "Square",
            ["Guardar cambios"] = "Save",
            ["Eliminar alerta"] = "Trash",
            ["Agregar audio"] = "Plus",
            ["Guardar audio"] = "Save",
            ["Nuevo grupo"] = "Plus",
            ["Buscar"] = "Search",
            ["Arduino Tira led ws2812b"] = "Arduino",
            ["Alexa"] = "Alexa",
            ["Aplicar fondo LED"] = "Sun",
            ["Apagar tiras"] = "Power",
            ["Borrar salida"] = "Trash",
            ["Agregar salida de pin digital"] = "Plus",
            ["Descargar ultimo sketch"] = "Download",
            ["Ver guia"] = "Book",
            ["Aplicar fondo Alexa"] = "Alexa",
            ["Apagar fondo Alexa"] = "Power",
            ["Exportar configuracion"] = "Upload",
            ["Importar configuracion"] = "Download",
            ["Crear backup ahora"] = "Save",
            ["Restaurar backup"] = "Download",
            ["Ejecutar diagnostico"] = "MonitorCheck",
            ["Limpiar actividad"] = "Trash",
            ["Limpiar filtros"] = "Search",
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
        var text = new TextBlock
        {
            Text = label,
            Margin = new Thickness(7, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        text.SetBinding(
            TextBlock.ForegroundProperty,
            new System.Windows.Data.Binding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(System.Windows.Controls.Button), 1)
            });
        panel.Children.Add(text);

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
            "Bits" => "M12,2 L20,9 L12,22 L4,9 Z M12,2 L12,22 M4,9 L20,9",
            "Book" => "M4,5 C6,4 8,4 10,5 L10,20 C8,19 6,19 4,20 Z M20,5 C18,4 16,4 14,5 L14,20 C16,19 18,19 20,20 Z M10,5 L14,5 M10,20 L14,20",
            "Chat" => "M4,5 L20,5 L20,16 L9,16 L5,20 L5,16 L4,16 Z M8,10 L16,10 M8,13 L13,13",
            "Copy" => "M8,8 L19,8 L19,19 L8,19 Z M5,15 L4,15 L4,4 L15,4 L15,5",
            "Download" => "M12,3 L12,15 M7,10 L12,15 L17,10 M5,20 L19,20",
            "Event" => "M12,3 L14.5,9 L21,9 L15.5,13 L17.5,21 L12,16.5 L6.5,21 L8.5,13 L3,9 L9.5,9 Z",
            "ExternalLink" => "M14,4 L20,4 L20,10 M20,4 L11,13 M19,14 L19,20 L5,20 L5,6 L11,6",
            "Home" => "M3,11 L12,3 L21,11 M5,10 L5,21 L10,21 L10,15 L14,15 L14,21 L19,21 L19,10",
            "MonitorCheck" => "M4,5 L20,5 L20,16 L4,16 Z M9,21 L15,21 M12,16 L12,21 M8,10 L11,13 L16,8",
            "Play" => "M8,5 L19,12 L8,19 Z",
            "Plug" => "M8,3 L8,9 M16,3 L16,9 M6,9 L18,9 L18,13 C18,16 16,18 13,18 L13,22 M10,22 L10,18 C7,18 5,16 5,13 L5,9",
            "Plus" => "M12,5 L12,19 M5,12 L19,12",
            "Power" => "M12,3 L12,11 M7,6 C5,8 4,10 4,13 C4,17 8,21 12,21 C16,21 20,17 20,13 C20,10 19,8 17,6",
            "Save" => "M5,4 L17,4 L20,7 L20,20 L4,20 L4,4 Z M8,4 L8,10 L16,10 L16,4 M8,20 L8,14 L16,14 L16,20",
            "Search" => "M10.5,5 A5.5,5.5 0 1 1 10.5,16 A5.5,5.5 0 1 1 10.5,5 M15,15 L21,21",
            "Settings" => "M12,8 A4,4 0 1 1 12,16 A4,4 0 1 1 12,8 M12,2 L14,2 L15,5 L18,4 L20,6 L19,9 L22,11 L22,13 L19,15 L20,18 L18,20 L15,19 L14,22 L10,22 L9,19 L6,20 L4,18 L5,15 L2,13 L2,11 L5,9 L4,6 L6,4 L9,5 L10,2 Z",
            "Square" => "M7,7 L17,7 L17,17 L7,17 Z",
            "Star" => "M12,3 L14.6,8.6 L20.8,9.3 L16.2,13.5 L17.5,19.8 L12,16.6 L6.5,19.8 L7.8,13.5 L3.2,9.3 L9.4,8.6 Z",
            "Sun" => "M12,7 A5,5 0 1 1 12,17 A5,5 0 1 1 12,7 M12,1 L12,4 M12,20 L12,23 M4.2,4.2 L6.3,6.3 M17.7,17.7 L19.8,19.8 M1,12 L4,12 M20,12 L23,12 M4.2,19.8 L6.3,17.7 M17.7,6.3 L19.8,4.2",
            "Trash" => "M4,7 L20,7 M9,7 L9,5 L15,5 L15,7 M7,7 L8,21 L16,21 L17,7 M10,11 L10,18 M14,11 L14,18",
            "Twitch" => "M4,5 L20,5 L20,16 L13,16 L9,20 L9,16 L4,16 Z M8,9 L8,13 M13,9 L13,13",
            "Upload" => "M12,15 L12,3 M7,8 L12,3 L17,8 M5,20 L19,20",
            "Users" => "M8,11 A4,4 0 1 1 8,3 A4,4 0 1 1 8,11 M2,21 C2,16 5,14 8,14 C11,14 14,16 14,21 M17,10 A3,3 0 1 1 17,4 A3,3 0 1 1 17,10 M15,14 C18,14 21,16 21,20",
            "Warning" => "M12,3 L22,20 L2,20 Z M12,8 L12,13 M12,17 L12.1,17",
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

        ApplyStartWithWindowsRegistration();
        _ = CheckForUpdatesAsync();


        if (_config.StartHidden)
        {
            Hide();
        }

        if (_config.ArduinoEnabled && _config.AutoConnectArduino && !string.IsNullOrWhiteSpace(_config.SerialPort))
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

    private void OpenTwitchConsoleButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://dev.twitch.tv/console/apps",
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
            FileName = $"https://www.twitch.tv/{Uri.EscapeDataString(channel)}",
            UseShellExecute = true
        });
        AddLog($"Twitch: abriendo perfil de {channel}.", ActivityLogKind.Twitch);
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

    private void OpenArduinoSketchButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/Dafovi/NeoTwtich/blob/main/NeoTwitch/Arduino/NeoTwitchNeoPixel/NeoTwitchNeoPixel.ino",
            UseShellExecute = true
        });
        AddLog("Arduino: abriendo sketch NeoPixel.", ActivityLogKind.Arduino);
    }

    private void OpenArduinoGuideButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/Dafovi/NeoTwtich#conexion-arduino-y-neopixel",
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
            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = $"--update --target \"{installPath}\" --version \"V{result.LatestVersion}\"",
                WorkingDirectory = System.IO.Path.GetDirectoryName(installerPath),
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

    private static string FindLocalInstallerPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            System.IO.Path.Combine(baseDirectory, "NeoTwitch.Installer.exe"),
            System.IO.Path.Combine(baseDirectory, "Installer", "NeoTwitch.Installer.exe"),
            System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NeoTwitch",
                "Updater",
                "NeoTwitch.Installer.exe"),
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
        }
        finally
        {
            _isArduinoConnecting = false;
            UpdateStatusText();
        }
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

    private void CreateBackupButton_Click(object sender, RoutedEventArgs e)
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

    private async void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
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
        var (text, color, imagePath) = state switch
        {
            ConnectionVisualState.Connected => ("Estado: Todo en orden", "#22C55E", "Assets/Icons/appstate_ok.png"),
            ConnectionVisualState.Warning => ("Estado: Hay puntos por revisar", "#FFB020", "Assets/Icons/appstate_warning.png"),
            _ => ("Estado: Revisa el diagnostico", "#F43F5E", "Assets/Icons/appstate_error.png")
        };

        SettingsAppStateIcon.Source = LoadPackImage(imagePath);
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
        ShowAllRuleFilters();
        RefreshRulesView();
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
        ShowAllRuleFilters();
        RefreshRulesView();
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

        RemoveRule(rule);
    }

    private void RemoveRule(EventRule rule)
    {
        if (WpfMessageBox.Show(this, $"Eliminar la alerta '{rule.Name}'?", "Alertas", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        var wasSelected = ReferenceEquals(RulesList.SelectedItem, rule);
        _config.Rules.Remove(rule);
        RefreshRulesView();

        if (_config.Rules.Count > 0)
        {
            if (wasSelected || RulesList.SelectedItem is not EventRule)
            {
                RulesList.SelectedItem = _rulesViewSource.View?.Cast<EventRule>().FirstOrDefault();
            }
        }
        else
        {
            LoadSelectedRuleIntoUi();
        }

        SaveConfig();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
    }

    private async void RuleTestButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentEffectCts is not null)
            {
                await StopCurrentEffectAsync();
                UpdateRuleTestButtonState();
                return;
            }

            await StartRuleTestAsync();
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo probar la alerta.");
            AddLog($"Prueba de alerta: {ex.Message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "Probar alerta", MessageBoxButton.OK, MessageBoxImage.Warning);
            UpdateRuleTestButtonState();
        }
    }

    private async Task StartRuleTestAsync()
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

    private void UpdateRuleTestButtonState()
    {
        if (_initializingComponent)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateRuleTestButtonState);
            return;
        }

        var isRunning = _currentEffectCts is not null;
        RuleTestButton.Style = isRunning
            ? Resources["DangerButton"] as Style
            : Resources["PrimaryButton"] as Style;
        SetButtonIcon(RuleTestButton, isRunning ? "Parar prueba" : "Probar alerta", isRunning ? "Square" : "Play");
        ApplyButtonTheme(RuleTestButton, _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light);
    }

    private TwitchEvent BuildSimulatedEvent(EventRule rule)
    {
        var kind = rule.EventKind == TwitchEventKind.Test
            ? TwitchEventKind.Follow
            : rule.EventKind;
        var userName = "Prueba";
        var bits = Math.Max(1, rule.MinimumBits);
        var viewers = 18;
        var rewardTitle = FirstNonEmpty(rule.CustomRewardTitle, "Canje de prueba");
        var message = kind == TwitchEventKind.ChatCommand
            ? FirstNonEmpty(rule.ChatCommand, "!baile mensaje de prueba")
            : "Mensaje de prueba";

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
        if (rule.PlayAudio && !RuleHasValidAudio(rule))
        {
            var message = $"El audio de '{rule.Name}' no existe o no esta configurado.";
            AddLog($"Simulador: {message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, message, "Simulador de eventos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (_config.ArduinoEnabled && rule.UseLights && !_lightController.HasOpenPort)
        {
            AddLog(
                string.IsNullOrWhiteSpace(_config.SerialPort)
                    ? "Simulador: la regla usa luces, pero no hay puerto COM configurado."
                    : $"Simulador: la regla usa luces, pero Arduino no esta conectado ahora ({_config.SerialPort}).",
                ActivityLogKind.Important);
        }

        if (_config.ArduinoEnabled && rule.UseLights && !string.IsNullOrWhiteSpace(rule.TargetPins) && LightCommand.ParsePins(rule.TargetPins).Count == 0)
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

    private void RuleAudioModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.Tag is not string value
            || !Enum.TryParse<AudioSourceMode>(value, out var mode))
        {
            return;
        }

        _ruleAudioMode = mode;
        UpdateRuleAudioModeSelection();
        UpdateRuleOptionVisibility();
        SaveCurrentRuleFromFields();
        SaveConfig();
    }

    private void AudioSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingUi || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        _audioSearchText = textBox.Text.Trim();
        RefreshAudioLibraryView();
    }

    private void AudioFilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button)
        {
            return;
        }

        _audioFilter = button.Tag?.ToString() ?? "ALL";
        _audioGroupFilterId = "";
        UpdateAudioFilterButtons();
        RefreshAudioLibraryView();
    }

    private void AudioLibraryGroupBox_DropDownClosed(object sender, EventArgs e)
    {
        if (_refreshingAudioLibrary
            || _loadingUi
            || sender is not System.Windows.Controls.ComboBox comboBox
            || comboBox.Tag is not string audioId)
        {
            return;
        }

        var audio = _config.AudioLibrary.FirstOrDefault(item => string.Equals(item.Id, audioId, StringComparison.OrdinalIgnoreCase));
        if (audio is null)
        {
            return;
        }

        var selectedGroupId = comboBox.SelectedValue as string ?? "";
        if (string.Equals(audio.GroupId, selectedGroupId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        audio.GroupId = selectedGroupId;
        SaveConfig();
        _ = Dispatcher.InvokeAsync(() =>
        {
            RefreshAudioLibraryView();
            RefreshRulesView();
        }, DispatcherPriority.Background);
    }

    private void BrowseNewAudioButton_Click(object sender, RoutedEventArgs e)
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

        _newAudioPath = dialog.FileName;
        NewAudioPathBox.Text = dialog.FileName;
        if (string.IsNullOrWhiteSpace(NewAudioNameBox.Text))
        {
            NewAudioNameBox.Text = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
        }
    }

    private async void SaveNewAudioButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_newAudioPath) || !File.Exists(_newAudioPath))
        {
            WpfMessageBox.Show(this, "Selecciona un archivo de audio valido.", "Audio", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var existing = _config.AudioLibrary.FirstOrDefault(audio =>
            string.Equals(audio.FilePath, _newAudioPath, StringComparison.OrdinalIgnoreCase));
        var audio = existing ?? new AudioAssetConfig { FilePath = _newAudioPath };
        audio.Name = string.IsNullOrWhiteSpace(NewAudioNameBox.Text)
            ? System.IO.Path.GetFileNameWithoutExtension(_newAudioPath)
            : NewAudioNameBox.Text.Trim();
        audio.GroupId = NewAudioGroupBox.SelectedValue as string ?? "";

        var duration = await _audioPlayer.ProbeDurationAsync(_newAudioPath);
        if (duration is { TotalMilliseconds: > 0 })
        {
            audio.DurationMs = (int)Math.Round(duration.Value.TotalMilliseconds);
        }

        if (existing is null)
        {
            _config.AudioLibrary.Add(audio);
        }

        var selectedRuleId = NewAudioAlertBox.SelectedValue as string ?? "";
        var rule = _config.Rules.FirstOrDefault(item => string.Equals(item.Id, selectedRuleId, StringComparison.OrdinalIgnoreCase));
        if (rule is not null)
        {
            rule.PlayAudio = true;
            rule.AudioSourceMode = AudioSourceMode.Single;
            rule.AudioAssetId = audio.Id;
            rule.AudioGroupId = "";
            rule.AudioPath = audio.FilePath;
            if (ReferenceEquals(RulesList.SelectedItem, rule))
            {
                LoadSelectedRuleIntoUi();
            }
        }

        NewAudioPathBox.Text = "";
        NewAudioNameBox.Text = "";
        NewAudioAlertBox.SelectedValue = "";
        NewAudioGroupBox.SelectedValue = "";
        _newAudioPath = "";

        SaveConfig();
        RefreshAudioLibraryView();
        RefreshRulesView();
        AddLog($"Audio: guardado {audio.DisplayName}.", ActivityLogKind.Audio);
    }

    private void AddAudioGroupButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NewAudioGroupNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            WpfMessageBox.Show(this, "Escribe un nombre para el grupo.", "Audio", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var existing = _config.AudioGroups.FirstOrDefault(group =>
            string.Equals(group.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            NewAudioGroupBox.SelectedValue = existing.Id;
            NewAudioGroupNameBox.Text = "";
            return;
        }

        var group = new AudioGroupConfig { Name = name };
        _config.AudioGroups.Add(group);
        NewAudioGroupBox.SelectedValue = group.Id;
        NewAudioGroupNameBox.Text = "";

        SaveConfig();
        RefreshAudioLibraryView();
        UpdateRuleOptionVisibility();
        AddLog($"Audio: grupo creado {group.Name}.", ActivityLogKind.Audio);
    }

    private void ViewAudioGroupButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string groupId)
        {
            return;
        }

        var group = _config.AudioGroups.FirstOrDefault(item => string.Equals(item.Id, groupId, StringComparison.OrdinalIgnoreCase));
        if (group is null)
        {
            return;
        }

        _audioGroupFilterId = group.Id;
        _audioFilter = "ALL";
        AudioSearchBox.Text = "";
        _audioSearchText = "";
        UpdateAudioFilterButtons();
        RefreshAudioLibraryView();
        AddLog($"Audio: mostrando grupo {group.Name}.", ActivityLogKind.Audio);
    }

    private void DeleteAudioGroupButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string groupId)
        {
            return;
        }

        var group = _config.AudioGroups.FirstOrDefault(item => string.Equals(item.Id, groupId, StringComparison.OrdinalIgnoreCase));
        if (group is null)
        {
            return;
        }

        var audioCount = _config.AudioLibrary.Count(audio => string.Equals(audio.GroupId, group.Id, StringComparison.OrdinalIgnoreCase));
        if (WpfMessageBox.Show(
                this,
                $"Eliminar el grupo '{group.Name}'?\n\nLos {audioCount} audio(s) no se borran; solo quedaran sin grupo.",
                "Audio",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        foreach (var audio in _config.AudioLibrary.Where(audio => string.Equals(audio.GroupId, group.Id, StringComparison.OrdinalIgnoreCase)))
        {
            audio.GroupId = "";
        }

        foreach (var rule in _config.Rules.Where(rule => rule.AudioSourceMode == AudioSourceMode.Group
                     && string.Equals(rule.AudioGroupId, group.Id, StringComparison.OrdinalIgnoreCase)))
        {
            rule.AudioGroupId = "";
            rule.PlayAudio = false;
        }

        _config.AudioGroups.Remove(group);
        if (string.Equals(_audioGroupFilterId, group.Id, StringComparison.OrdinalIgnoreCase))
        {
            _audioGroupFilterId = "";
        }

        SaveConfig();
        RefreshAudioLibraryView();
        RefreshRulesView();
        LoadSelectedRuleIntoUi();
        AddLog($"Audio: grupo eliminado {group.Name}.", ActivityLogKind.Audio);
    }

    private async void PreviewAudioButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string audioId)
        {
            return;
        }

        var audio = _config.AudioLibrary.FirstOrDefault(item => string.Equals(item.Id, audioId, StringComparison.OrdinalIgnoreCase));
        if (audio is null)
        {
            return;
        }

        if (_audioPreviewPlayback is not null && string.Equals(_previewingAudioId, audio.Id, StringComparison.OrdinalIgnoreCase))
        {
            _audioPreviewPlayback.Stop();
            ClearAudioPreviewState(audio.Id);
            return;
        }

        var playback = await _audioPlayer.PrepareAsync(audio.FilePath, _config.AlertVolumePercent, AddLog);
        if (playback is null)
        {
            return;
        }

        _audioPreviewPlayback?.Stop();
        _audioPreviewPlayback = playback;
        _previewingAudioId = audio.Id;
        MarkAudioAssetUsed(audio, playback.Duration);
        playback.Play();
        AddLog($"Audio: reproduciendo {audio.DisplayName}.", ActivityLogKind.Audio);
        _ = WatchAudioPreviewCompletionAsync(playback, audio.Id);
    }

    private void DeleteAudioButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string audioId)
        {
            return;
        }

        var audio = _config.AudioLibrary.FirstOrDefault(item => string.Equals(item.Id, audioId, StringComparison.OrdinalIgnoreCase));
        if (audio is null)
        {
            return;
        }

        if (WpfMessageBox.Show(this, $"Eliminar el audio '{audio.DisplayName}' de la biblioteca?", "Audio", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        if (string.Equals(_previewingAudioId, audio.Id, StringComparison.OrdinalIgnoreCase))
        {
            _audioPreviewPlayback?.Stop();
            ClearAudioPreviewState(audio.Id);
        }

        _config.AudioLibrary.Remove(audio);
        foreach (var rule in _config.Rules.Where(rule => string.Equals(rule.AudioAssetId, audio.Id, StringComparison.OrdinalIgnoreCase)))
        {
            rule.AudioAssetId = "";
            rule.AudioPath = "";
            rule.PlayAudio = rule.AudioSourceMode == AudioSourceMode.Group && !string.IsNullOrWhiteSpace(rule.AudioGroupId);
        }

        SaveConfig();
        RefreshAudioLibraryView();
        RefreshRulesView();
        LoadSelectedRuleIntoUi();
    }

    private async Task WatchAudioPreviewCompletionAsync(AudioPlayback playback, string audioId)
    {
        try
        {
            await playback.Completion;
        }
        finally
        {
            await Dispatcher.InvokeAsync(() => ClearAudioPreviewState(audioId));
        }
    }

    private void ClearAudioPreviewState(string audioId)
    {
        if (!string.Equals(_previewingAudioId, audioId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _audioPreviewPlayback = null;
        _previewingAudioId = "";
        RefreshAudioLibraryView();
    }

    private void StopAudioPreview()
    {
        if (_audioPreviewPlayback is null)
        {
            return;
        }

        var audioId = _previewingAudioId;
        _audioPreviewPlayback.Stop();
        ClearAudioPreviewState(audioId);
    }

    private bool RuleHasValidAudio(EventRule rule)
    {
        if (!Dispatcher.CheckAccess())
        {
            return Dispatcher.Invoke(() => RuleHasValidAudio(rule));
        }

        var asset = ResolveRuleAudioAsset(rule);
        if (asset is not null)
        {
            return File.Exists(asset.FilePath);
        }

        return rule.AudioSourceMode == AudioSourceMode.Single
            && !string.IsNullOrWhiteSpace(rule.AudioPath)
            && File.Exists(rule.AudioPath);
    }

    private AudioAssetConfig? ResolveRuleAudioAsset(EventRule rule)
    {
        if (!Dispatcher.CheckAccess())
        {
            return Dispatcher.Invoke(() => ResolveRuleAudioAsset(rule));
        }

        if (!rule.PlayAudio)
        {
            return null;
        }

        if (rule.AudioSourceMode == AudioSourceMode.Group)
        {
            var candidates = _config.AudioLibrary
                .Where(audio => string.Equals(audio.GroupId, rule.AudioGroupId, StringComparison.OrdinalIgnoreCase))
                .Where(audio => File.Exists(audio.FilePath))
                .ToArray();
            return candidates.Length == 0
                ? null
                : candidates[_audioRandom.Next(candidates.Length)];
        }

        return _config.AudioLibrary.FirstOrDefault(audio => string.Equals(audio.Id, rule.AudioAssetId, StringComparison.OrdinalIgnoreCase))
            ?? _config.AudioLibrary.FirstOrDefault(audio => !string.IsNullOrWhiteSpace(rule.AudioPath)
                && string.Equals(audio.FilePath, rule.AudioPath, StringComparison.OrdinalIgnoreCase));
    }

    private void MarkAudioAssetUsed(AudioAssetConfig audio, TimeSpan? duration)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => MarkAudioAssetUsed(audio, duration));
            return;
        }

        if (duration is { TotalMilliseconds: > 0 })
        {
            audio.DurationMs = (int)Math.Round(duration.Value.TotalMilliseconds);
        }

        audio.LastUsedAt = DateTimeOffset.Now;
        SaveConfig();
        RefreshAudioLibraryView();
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
        ApplyStartWithWindowsRegistration();
        UpdateSensitiveFieldVisibility();
        UpdateSliderLabels();
        UpdateStatusText();
        UpdateRuleOptionVisibility();
        ApplyBackgroundOutputMode();
        UpdateCloseBehaviorCards();
    }

    private void CloseBehaviorRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        CloseToTrayCheck.IsChecked = sender == CloseToTrayRadio;
        GlobalSettingsChanged(sender, e);
    }

    private void AlexaSettingsChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        _alexaRelayConnected = false;
        SaveConfig();
        UpdateAlexaStatusText();
        UpdateSensitiveFieldVisibility();
        UpdateRuleOptionVisibility();
    }

    private async void TestAlexaButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _isAlexaConnecting = true;
            UpdateStatusText();
            SaveGlobalSettingsFromFields();
            SaveConfig();
            await _alexaRelayService.SendTestEventAsync(_config, CancellationToken.None);
            _alexaRelayConnected = true;
            AddLog("Alexa: evento de prueba enviado.", ActivityLogKind.Alexa);
        }
        catch (Exception ex)
        {
            _alexaRelayConnected = false;
            CrashReporter.Log(ex, "No se pudo enviar la prueba de Alexa.");
            AddLog($"Alexa: {ex.Message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "Alexa", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _isAlexaConnecting = false;
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
        UpdateEventKindTileSelection();
        UpdatePatternTileSelection();
        UpdateRuleOptionVisibility();
        UpdateRuleLedPreviewTimerState();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
    }

    private void EventKindTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.Tag is not string value
            || !Enum.TryParse<TwitchEventKind>(value, out var kind))
        {
            return;
        }

        EventKindBox.SelectedValue = kind;
        UpdateEventKindTileSelection();
    }

    private void PatternTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.Tag is not string value
            || !Enum.TryParse<LightPattern>(value, out var pattern))
        {
            return;
        }

        PatternBox.SelectedValue = pattern;
        UpdatePatternTileSelection();
        UpdateRuleLedPreviewFrame();
    }

    private void BackgroundPatternTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.Tag is not string value
            || !Enum.TryParse<LightPattern>(value, out var pattern))
        {
            return;
        }

        BackgroundPatternBox.SelectedValue = pattern;
        UpdateBackgroundPatternTileSelection();
        UpdateBackgroundLedPreviewFrame();
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
        UpdateBackgroundPatternTileSelection();
        UpdateBackgroundLedPreviewFrame();
        UpdateBackgroundLedPreviewTimerState();
    }

    private void RuleLedPreviewPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateRuleLedPreviewTimerState();
    }

    private void BackgroundLedPreviewPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateBackgroundLedPreviewTimerState();
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
        UpdateCloseBehaviorCards();
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
        if (int.TryParse(NavAudioButton.Tag?.ToString(), out var audioTabIndex)
            && MainTabs.SelectedIndex != audioTabIndex)
        {
            StopAudioPreview();
        }

        UpdateRuleLedPreviewTimerState();
        UpdateBackgroundLedPreviewTimerState();
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

    private void GoToActivityButton_Click(object sender, RoutedEventArgs e)
    {
        MainTabs.SelectedIndex = 7;
        UpdateNavigationButtons();
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

    private void RuleSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingUi || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        _ruleSearchText = textBox.Text.Trim();
        RefreshRulesView();
    }

    private void RuleStatusFilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (_loadingUi || sender is not ToggleButton button)
        {
            return;
        }

        button.IsChecked = true;
        _ruleStatusFilter = button.Tag?.ToString() ?? "ALL";

        foreach (var filterButton in RuleStatusFilterButtons())
        {
            if (!ReferenceEquals(filterButton, button))
            {
                filterButton.IsChecked = false;
            }

            ApplyRuleStatusFilterButtonTheme(filterButton, _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light);
        }

        RefreshRulesView();
    }

    private void RuleCategoryFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingUi)
        {
            return;
        }

        _ruleCategoryFilter = RuleCategoryFilterBox.SelectedValue?.ToString() ?? "";
        RefreshRulesView();
    }

    private void RulesViewSource_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not EventRule rule)
        {
            e.Accepted = false;
            return;
        }

        e.Accepted = RuleMatchesFilters(rule);
    }

    private bool RuleMatchesFilters(EventRule rule)
    {
        if (_ruleStatusFilter == "ACTIVE" && !rule.IsEnabled)
        {
            return false;
        }

        if (_ruleStatusFilter == "INACTIVE" && rule.IsEnabled)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_ruleCategoryFilter)
            && !string.Equals(rule.EventKind.ToString(), _ruleCategoryFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_ruleSearchText))
        {
            return true;
        }

        var text = _ruleSearchText;
        return ContainsIgnoreCase(rule.Name, text)
            || ContainsIgnoreCase(rule.DisplayLabel, text)
            || ContainsIgnoreCase(rule.ChatCommand, text)
            || ContainsIgnoreCase(rule.CustomRewardTitle, text)
            || ContainsIgnoreCase(rule.ChatMessageTemplate, text)
            || ContainsIgnoreCase(DisplayNames.For(rule.EventKind), text);
    }

    private static bool ContainsIgnoreCase(string? value, string query)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshRulesView()
    {
        var selected = RulesList.SelectedItem as EventRule;
        _rulesViewSource.View?.Refresh();

        if (selected is not null && _rulesViewSource.View?.Contains(selected) == true)
        {
            RulesList.SelectedItem = selected;
        }
        else if (RulesList.SelectedItem is not EventRule)
        {
            RulesList.SelectedItem = _rulesViewSource.View?.Cast<EventRule>().FirstOrDefault();
        }

        UpdateRulesCountText();
    }

    private void UpdateRulesCountText()
    {
        if (_initializingComponent || RulesCountText is null)
        {
            return;
        }

        var visibleCount = _rulesViewSource.View?.Cast<EventRule>().Count() ?? 0;
        RulesCountText.Text = $"Mostrando {visibleCount} de {_config.Rules.Count} alertas";
    }

    private void RefreshAudioLibraryView()
    {
        if (_initializingComponent)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(RefreshAudioLibraryView);
            return;
        }

        _refreshingAudioLibrary = true;
        try
        {
            var groupsById = _config.AudioGroups.ToDictionary(group => group.Id, group => group.Name, StringComparer.OrdinalIgnoreCase);

            RefreshAudioGroupChoicesIfNeeded();
            RefreshAudioAlertChoicesIfNeeded();

            var rows = _config.AudioLibrary
                .Select((audio, index) => CreateAudioLibraryRow(audio, groupsById, index))
                .Where(AudioRowMatchesFilters)
                .ToArray();

            _audioLibraryRows.Clear();
            foreach (var row in rows)
            {
                _audioLibraryRows.Add(row);
            }

            _audioGroupRows.Clear();
            var groupIndex = 0;
            foreach (var group in _config.AudioGroups)
            {
                var count = _config.AudioLibrary.Count(audio => string.Equals(audio.GroupId, group.Id, StringComparison.OrdinalIgnoreCase));
                _audioGroupRows.Add(new AudioGroupRow(
                    group.Id,
                    group.Name,
                    $"{count} audio{(count == 1 ? "" : "s")}",
                    FrozenBrushFrom((groupIndex++ % 4) switch
                    {
                        0 => "#14B8A6",
                        1 => "#B56CFF",
                        2 => "#37C7F3",
                        _ => "#22C55E"
                    })));
            }

            AudioSavedCountText.Text = _config.AudioLibrary.Count.ToString();
            AudioGroupCountText.Text = _config.AudioGroups.Count.ToString();
            var lastAudio = _config.AudioLibrary
                .Where(audio => audio.LastUsedAt is not null)
                .OrderByDescending(audio => audio.LastUsedAt)
                .FirstOrDefault();
            LastAudioText.Text = lastAudio?.DisplayName ?? "Sin uso";
            var groupFilterText = string.IsNullOrWhiteSpace(_audioGroupFilterId)
                ? ""
                : $" del grupo {groupsById.GetValueOrDefault(_audioGroupFilterId, "seleccionado")}";
            AudioLibraryFooterText.Text = $"Mostrando {rows.Length} de {_config.AudioLibrary.Count} audios{groupFilterText}";

            RuleAudioAssetBox.Items.Refresh();
            RuleAudioGroupBox.Items.Refresh();
            NewAudioAlertBox.Items.Refresh();
            NewAudioGroupBox.Items.Refresh();
            UpdateAudioFilterButtons();
        }
        finally
        {
            _refreshingAudioLibrary = false;
        }
    }

    private void RefreshAudioGroupChoicesIfNeeded()
    {
        var signature = string.Join("|", _config.AudioGroups.Select(group => $"{group.Id}:{group.Name}"));
        if (string.Equals(signature, _audioGroupChoicesSignature, StringComparison.Ordinal))
        {
            return;
        }

        AudioGroupChoices.Clear();
        AudioGroupChoices.Add(new AudioGroupChoice("", "Sin grupo"));
        foreach (var group in _config.AudioGroups)
        {
            AudioGroupChoices.Add(new AudioGroupChoice(group.Id, group.Name));
        }

        _audioGroupChoicesSignature = signature;
    }

    private void RefreshAudioAlertChoicesIfNeeded()
    {
        var signature = string.Join("|", _config.Rules.Select(rule => $"{rule.Id}:{rule.Name}"));
        if (string.Equals(signature, _audioAlertChoicesSignature, StringComparison.Ordinal))
        {
            return;
        }

        AudioAlertChoices.Clear();
        AudioAlertChoices.Add(new AudioAlertChoice("", "Sin alerta asignada"));
        foreach (var rule in _config.Rules)
        {
            AudioAlertChoices.Add(new AudioAlertChoice(rule.Id, string.IsNullOrWhiteSpace(rule.Name) ? rule.DisplayLabel : rule.Name));
        }

        _audioAlertChoicesSignature = signature;
    }

    private AudioLibraryRow CreateAudioLibraryRow(AudioAssetConfig audio, IReadOnlyDictionary<string, string> groupsById, int index)
    {
        var assignedRules = _config.Rules
            .Where(rule => RuleUsesAudioAsset(rule, audio))
            .ToArray();
        var assignedText = assignedRules.Length switch
        {
            0 => "",
            1 => assignedRules[0].Name,
            _ => $"{assignedRules[0].Name} +{assignedRules.Length - 1}"
        };
        var accentColor = assignedRules.Length > 0
            ? EventKindAccent(assignedRules[0].EventKind)
            : "#64748B";

        return new AudioLibraryRow(
            audio.Id,
            audio.DisplayName,
            audio.FilePath,
            audio.GroupId,
            assignedText,
            groupsById.TryGetValue(audio.GroupId, out var groupName) ? groupName : "Sin grupo",
            audio.DurationText,
            assignedRules.Length > 0,
            string.Equals(_previewingAudioId, audio.Id, StringComparison.OrdinalIgnoreCase) && _audioPreviewPlayback is not null,
            FrozenBrushFrom(accentColor),
            TranslucentBrushFrom(accentColor),
            index);
    }

    private bool AudioRowMatchesFilters(AudioLibraryRow row)
    {
        if (!string.IsNullOrWhiteSpace(_audioGroupFilterId)
            && !string.Equals(row.GroupId, _audioGroupFilterId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (_audioFilter == "WITH_ALERT" && !row.HasAssignedAlert)
        {
            return false;
        }

        if (_audioFilter == "NO_GROUP" && !string.Equals(row.GroupName, "Sin grupo", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_audioSearchText))
        {
            return true;
        }

        return ContainsIgnoreCase(row.Name, _audioSearchText)
            || ContainsIgnoreCase(row.FilePath, _audioSearchText)
            || ContainsIgnoreCase(row.AssignedAlertText, _audioSearchText)
            || ContainsIgnoreCase(row.GroupName, _audioSearchText);
    }

    private static bool RuleUsesAudioAsset(EventRule rule, AudioAssetConfig audio)
    {
        if (!rule.PlayAudio)
        {
            return false;
        }

        if (rule.AudioSourceMode == AudioSourceMode.Group)
        {
            return !string.IsNullOrWhiteSpace(rule.AudioGroupId)
                && string.Equals(rule.AudioGroupId, audio.GroupId, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(rule.AudioAssetId, audio.Id, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(rule.AudioPath)
                && string.Equals(rule.AudioPath, audio.FilePath, StringComparison.OrdinalIgnoreCase));
    }

    private void UpdateAudioFilterButtons()
    {
        if (_initializingComponent)
        {
            return;
        }

        var palette = _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light;
        foreach (var button in new[] { AudioFilterAllButton, AudioFilterWithAlertButton, AudioFilterNoGroupButton })
        {
            var active = string.Equals(button.Tag?.ToString(), _audioFilter, StringComparison.OrdinalIgnoreCase);
            button.Background = active ? TranslucentBrushFrom("#14B8A6") : palette.Input;
            button.Foreground = active ? FrozenBrushFrom("#14B8A6") : palette.Text;
            button.BorderBrush = active ? FrozenBrushFrom("#14B8A6") : palette.Border;
        }
    }

    private void ShowAllRuleFilters()
    {
        _ruleStatusFilter = "ALL";
        _ruleCategoryFilter = "";
        RuleFilterAllButton.IsChecked = true;
        RuleFilterActiveButton.IsChecked = false;
        RuleFilterInactiveButton.IsChecked = false;
        RuleCategoryFilterBox.SelectedValue = "";
        RuleSearchBox.Text = "";
        _ruleSearchText = "";
    }

    private void ActivityFilterButton_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || sender is not ToggleButton button)
        {
            return;
        }

        var filter = button.Tag?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(filter))
        {
            return;
        }

        if (button.IsChecked == true)
        {
            _activityEnabledFilters.Add(filter);
        }
        else
        {
            _activityEnabledFilters.Remove(filter);
        }

        ApplyActivityFilterButtonTheme(button, _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light);
        _activityViewSource.View?.Refresh();
    }

    private void ActivitySearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingUi || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        _activitySearchText = textBox.Text.Trim();
        _activityViewSource.View?.Refresh();
    }

    private void ClearActivityFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        _activityEnabledFilters.Clear();
        foreach (var button in ActivityFilterButtons())
        {
            var filter = button.Tag?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(filter))
            {
                _activityEnabledFilters.Add(filter);
            }

            button.IsChecked = true;
            ApplyActivityFilterButtonTheme(button, _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light);
        }

        ActivitySearchBox.Text = "";
        _activitySearchText = "";
        _activityViewSource.View?.Refresh();
    }

    private void ActivityViewSource_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not ActivityLogEntry entry)
        {
            e.Accepted = false;
            return;
        }

        e.Accepted = entry.MatchesFilter(_activityEnabledFilters, _activitySearchText);
    }

    private async void EventSubClient_EventReceived(TwitchEvent twitchEvent)
    {
        try
        {
            RegisterDashboardTwitchEvent(twitchEvent);
            var matchingRules = ResolveMatchingRules(twitchEvent);
            if (matchingRules.Length == 0)
            {
                if (twitchEvent.Kind != TwitchEventKind.ChatCommand)
                {
                    AddLog(twitchEvent.Title, ActivityLogKind.Event);
                    AddLog("El evento no coincide con alertas activas.");
                }

                return;
            }

            AddLog(twitchEvent.Title, ActivityLogKind.Event);
            RegisterDashboardMatchedRules(matchingRules.Length);

            foreach (var rule in matchingRules)
            {
                await QueueAndRunRuleAsync(rule, twitchEvent);
            }
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, $"No se pudo procesar evento Twitch '{twitchEvent.Title}'.");
            AddLog($"Twitch evento: {ex.Message}", ActivityLogKind.Important);
        }
    }

    private void RegisterDashboardMatchedRules(int count)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => RegisterDashboardMatchedRules(count));
            return;
        }

        _dashboardEventsToday += count;
        UpdateDashboardSummary();
    }

    private void RegisterDashboardTwitchEvent(TwitchEvent twitchEvent)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => RegisterDashboardTwitchEvent(twitchEvent));
            return;
        }

        switch (twitchEvent.Kind)
        {
            case TwitchEventKind.Follow:
                _dashboardFollowersToday++;
                break;
            case TwitchEventKind.Subscription:
                _dashboardSubscriptionsToday++;
                break;
            case TwitchEventKind.Cheer:
                _dashboardBitsToday += Math.Max(0, twitchEvent.Bits ?? 0);
                break;
            case TwitchEventKind.ChatCommand:
                _dashboardChatMessagesToday++;
                break;
        }

        UpdateDashboardSummary();
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
            _alexaRelayConnected = true;
            AddLog($"Alexa: evento enviado para '{rule.Name}'.", ActivityLogKind.Alexa);
        }
        catch (Exception ex)
        {
            _alexaRelayConnected = false;
            CrashReporter.Log(ex, $"No se pudo enviar evento Alexa para la regla '{rule.Name}'.");
            AddLog($"Alexa: {ex.Message}", ActivityLogKind.Important);
        }
        finally
        {
            UpdateAlexaStatusText();
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
        UpdateRuleTestButtonState();
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
            AudioAssetConfig? playbackAsset = null;
            if (rule.PlayAudio)
            {
                playbackAsset = ResolveRuleAudioAsset(rule);
                var audioPath = playbackAsset?.FilePath ?? rule.AudioPath;
                playback = await _audioPlayer.PrepareAsync(audioPath, _config.AlertVolumePercent, AddLog);
                _currentPlayback = playback;
                if (playbackAsset is not null)
                {
                    MarkAudioAssetUsed(playbackAsset, playback?.Duration);
                }
            }

            var useLights = _config.ArduinoEnabled && rule.UseLights;

            if (!useLights)
            {
                playback?.Play();
                if (playback is not null)
                {
                    await playback.Completion.WaitAsync(effectCts.Token);
                }

                return;
            }

            if (useLights && !_lightController.HasOpenPort && !string.IsNullOrWhiteSpace(_config.SerialPort))
            {
                await ConnectArduinoAsync();
            }

            shouldRestoreBackground = true;
            var targets = LightCommand.ResolveTargets(_config, rule.TargetPins);
            if (useLights)
            {
                await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
                await Task.Delay(LightStopSettleMs);
            }

            var audioDuration = playback?.Duration;
            var syncedDurationMs = audioDuration is { TotalMilliseconds: > 0 }
                ? (int)Math.Round(audioDuration.Value.TotalMilliseconds)
                : (int?)null;

            LightCommand? command = null;
            if (useLights)
            {
                command = LightCommand.FromRule(rule, _config, syncedDurationMs);
                await _lightController.SendAsync(command, AddLog, CancellationToken.None);
                UpdateStatusText();
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
            UpdateRuleTestButtonState();

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
            ArduinoEnabledCheck.IsChecked = _config.ArduinoEnabled;
            AutoTwitchCheck.IsChecked = _config.AutoConnectTwitch;
            AutoArduinoCheck.IsChecked = _config.AutoConnectArduino;
            StartHiddenCheck.IsChecked = _config.StartHidden;
            StartWithWindowsCheck.IsChecked = _config.StartWithWindows;
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
            _rulesViewSource.Source = _config.Rules;
            RulesList.ItemsSource = _rulesViewSource.View;
            RuleAudioAssetBox.ItemsSource = _config.AudioLibrary;
            RuleAudioGroupBox.ItemsSource = _config.AudioGroups;
            NewAudioAlertBox.ItemsSource = AudioAlertChoices;
            NewAudioGroupBox.ItemsSource = AudioGroupChoices;
            RefreshRulesView();
            StripsList.ItemsSource = _config.LedStrips;
            SettingsPathText.Text = _settingsStore.SettingsPath;
            BackupPathText.Text = $"Backups automaticos: {_settingsStore.BackupDirectory}";
            SettingsVersionText.Text = $"V{VersionCheckService.CurrentVersionText}";
            UpdateCloseBehaviorCards();

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
            UpdateBackgroundPatternTileSelection();
            UpdateBackgroundLedPreviewFrame();
            RefreshAudioLibraryView();
            UpdateAudioFilterButtons();
            UpdateLightsArduinoStatus();
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
            UpdateEventKindTileSelection();
            RewardTitleBox.Text = rule.CustomRewardTitle;
            ChatCommandBox.Text = rule.ChatCommand;
            MinimumBitsBox.Text = rule.MinimumBits.ToString();
            ChatMessageCheck.IsChecked = rule.SendChatMessage;
            ChatMessageBox.Text = rule.ChatMessageTemplate;
            AlexaEventCheck.IsChecked = rule.SendAlexaEvent;
            UseLightsCheck.IsChecked = rule.UseLights;
            PlayAudioCheck.IsChecked = rule.PlayAudio;
            _ruleAudioMode = rule.AudioSourceMode;
            RuleAudioAssetBox.SelectedValue = rule.AudioAssetId;
            RuleAudioGroupBox.SelectedValue = rule.AudioGroupId;
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
            UpdatePatternTileSelection();
            UpdateRuleLedPreviewFrame();
        }
        finally
        {
            _loadingRule = false;
            UpdateRuleOptionVisibility();
            UpdateRuleLedPreviewTimerState();
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
            UpdateLightsArduinoStatus();
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
        _config.ArduinoEnabled = ArduinoEnabledCheck.IsChecked == true;
        _config.AutoConnectTwitch = AutoTwitchCheck.IsChecked == true;
        _config.AutoConnectArduino = AutoArduinoCheck.IsChecked == true;
        _config.StartHidden = StartHiddenCheck.IsChecked == true;
        _config.StartWithWindows = StartWithWindowsCheck.IsChecked == true;
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

    private void ApplyStartWithWindowsRegistration()
    {
        if (_lastAppliedStartWithWindows == _config.StartWithWindows)
        {
            return;
        }

        try
        {
            using var runKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(WindowsRunKeyPath, writable: true);
            if (runKey is null)
            {
                throw new InvalidOperationException("No pude abrir la clave de inicio de Windows.");
            }

            if (_config.StartWithWindows)
            {
                var executablePath = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                {
                    throw new InvalidOperationException("No pude detectar la ruta del ejecutable actual.");
                }

                runKey.SetValue(WindowsStartupValueName, $"\"{executablePath}\"");
            }
            else
            {
                runKey.DeleteValue(WindowsStartupValueName, throwOnMissingValue: false);
            }

            _lastAppliedStartWithWindows = _config.StartWithWindows;
        }
        catch (Exception ex)
        {
            AddLog($"Inicio con Windows: {ex.Message}", ActivityLogKind.Important);
        }
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
        rule.AudioSourceMode = _ruleAudioMode;
        rule.AudioAssetId = RuleAudioAssetBox.SelectedValue as string ?? "";
        rule.AudioGroupId = RuleAudioGroupBox.SelectedValue as string ?? "";
        rule.AudioPath = rule.AudioSourceMode == AudioSourceMode.Single
            ? _config.AudioLibrary.FirstOrDefault(audio => string.Equals(audio.Id, rule.AudioAssetId, StringComparison.OrdinalIgnoreCase))?.FilePath ?? ""
            : "";
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
        UpdatePatternTileSelection();
        UpdateRuleAudioModeSelection();
        UpdateRuleLedPreviewFrame();
        UpdateRuleOptionVisibility();
        UpdateRuleLedPreviewTimerState();
        RefreshRulesView();
        RefreshAudioLibraryView();
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
        UpdateBackgroundPatternTileSelection();
        UpdateBackgroundLedPreviewFrame();
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
        RefreshRulesView();
        UpdateLightsArduinoStatus();
    }

    private void UpdateRuleOptionVisibility()
    {
        var kind = EventKindBox.SelectedValue is TwitchEventKind eventKind
            ? eventKind
            : TwitchEventKind.Follow;
        var arduinoAvailable = _config.ArduinoEnabled;
        var useLights = arduinoAvailable && UseLightsCheck.IsChecked == true;
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
        var hasAudios = _config.AudioLibrary.Count > 0;
        var hasGroups = _config.AudioGroups.Count > 0;
        SetVisible(playAudio, AudioDetailsPanel, AudioLabel, AudioPanel);
        SetVisible(playAudio && _ruleAudioMode == AudioSourceMode.Single && hasAudios, RuleAudioSinglePanel);
        SetVisible(playAudio && _ruleAudioMode == AudioSourceMode.Group && hasGroups, RuleAudioGroupPanel);
        SetVisible(playAudio && ((_ruleAudioMode == AudioSourceMode.Single && !hasAudios) || (_ruleAudioMode == AudioSourceMode.Group && !hasGroups)), RuleAudioEmptyHintText);
        SetVisible(sendChat, ChatDetailsPanel, ChatMessageLabel, ChatMessageBox);
        SetVisible(arduinoAvailable, UseLightsActionCard);
        SetVisible(alexaAvailable, AlexaActionCard);
        SetVisible(alexaAvailable && sendAlexa, AlexaDetailsPanel, AlexaRuleHintText);

        SetVisible(useLights, LightConfigurationPanel, LightOptionsSeparator, TargetPinsLabel, TargetPinsBox, PatternGrid, RuleLedPreviewPanel);
        SetVisible(useLights && UsesPrimaryColor(pattern), PrimaryColorPanel);
        SetVisible(useLights && UsesSecondaryColor(pattern), SecondaryColorLabel, SecondaryColorPanel);
        SetVisible(useLights && UsesTertiaryColor(pattern), TertiaryColorLabel, TertiaryColorPanel);
        SetVisible(useLights && UsesBrightness(pattern), BrightnessGrid, BrightnessSlider);
        SetVisible(useLights && !playAudio, DurationGrid, DurationSlider);
        SetVisible(useLights && UsesCycle(pattern), CycleGrid, CycleSlider);
        SetVisible(useLights && UsesStep(pattern), StepGrid, StepSlider);
        UpdateRuleAudioModeSelection();
        UpdateRuleLedPreviewFrame();
        UpdateRuleLedPreviewTimerState();
    }

    private void UpdateBackgroundOptionVisibility()
    {
        var arduinoAvailable = _config.ArduinoEnabled;
        var enabled = arduinoAvailable && BackgroundEnabledCheck.IsChecked == true;
        var alexaEnabled = BackgroundAlexaEnabledCheck.IsChecked == true;
        var alexaTurnOffAfterEvent = BackgroundAlexaTurnOffAfterEventCheck.IsChecked == true;
        var alexaAvailable = _config.Alexa.IsConfigured;
        var pattern = BackgroundPatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Solid;

        SetVisible(alexaAvailable, BackgroundAlexaEnabledCheck, BackgroundAlexaTurnOffAfterEventCheck, StopAlexaBackgroundButton);
        SetVisible(!alexaAvailable, AlexaBackgroundUnavailableText);
        SetVisible(alexaAvailable && (alexaEnabled || alexaTurnOffAfterEvent), BackgroundAlexaEventsGrid, ApplyAlexaBackgroundButton);
        SetVisible(arduinoAvailable, BackgroundEnabledCheck);
        SetVisible(enabled, BackgroundPatternGrid, BackgroundLedPreviewPanel, ApplyArduinoBackgroundButton);
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

        UpdateBackgroundOptionVisibility();
        UpdateBackgroundLedPreviewTimerState();
    }

    private static void SetVisible(bool isVisible, params UIElement[] elements)
    {
        var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        foreach (var element in elements)
        {
            element.Visibility = visibility;
        }
    }

    private void UpdateRuleLedPreviewFrame()
    {
        if (_initializingComponent || _ruleLedPreviewDots.Count == 0)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateRuleLedPreviewFrame);
            return;
        }

        if (!ShouldRunRuleLedPreview())
        {
            UpdateRuleLedPreviewTimerState();
            return;
        }

        var pattern = PatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Pulse;
        var brightness = Math.Clamp(BrightnessSlider.Value / 255d, 0d, 1d);
        var colorScale = Math.Clamp(brightness, 0.08, 1d);
        var primary = ParsePreviewColor(PrimaryColorBox.Text, "#14B8A6");
        var secondary = ParsePreviewColor(SecondaryColorBox.Text, "#B56CFF");
        var tertiary = ParsePreviewColor(TertiaryColorBox.Text, "#FFFFFF");
        var count = _ruleLedPreviewDots.Count;
        _ruleLedPreviewStep++;

        for (var i = 0; i < count; i++)
        {
            var phase = (i + _ruleLedPreviewStep) / (double)count;
            var color = pattern switch
            {
                LightPattern.Solid => primary,
                LightPattern.Rainbow => RainbowPreviewColor(phase),
                LightPattern.Pulse => BlendPreviewColor(primary, secondary, (Math.Sin((_ruleLedPreviewStep * 0.18) + (i * 0.22)) + 1d) / 2d),
                LightPattern.Chase => ((i + _ruleLedPreviewStep) % 6) < 2
                    ? primary
                    : ScalePreviewColor(secondary, 0.22),
                LightPattern.Theater => ((i + _ruleLedPreviewStep) % 3) == 0
                    ? primary
                    : (((i + _ruleLedPreviewStep) % 3) == 1 ? secondary : ScalePreviewColor(tertiary, 0.18)),
                LightPattern.Sparkle => _previewRandom.NextDouble() > 0.72
                    ? RandomPreviewColor(primary, secondary, tertiary)
                    : ScalePreviewColor(primary, 0.16),
                LightPattern.Rave => RandomPreviewColor(primary, secondary, tertiary),
                _ => primary
            };

            _ruleLedPreviewDots[i] = PreviewDot(ScalePreviewColor(color, colorScale), brightness);
        }
    }

    private void SetRuleLedPreviewAll(string color)
    {
        var previewColor = ParsePreviewColor(color, "#334155");
        for (var i = 0; i < _ruleLedPreviewDots.Count; i++)
        {
            _ruleLedPreviewDots[i] = PreviewDot(previewColor, 0.08);
        }
    }

    private void UpdateRuleLedPreviewTimerState()
    {
        if (_initializingComponent || !Dispatcher.CheckAccess())
        {
            return;
        }

        var shouldRun = ShouldRunRuleLedPreview();
        if (shouldRun)
        {
            if (!_ruleLedPreviewTimer.IsEnabled)
            {
                _ruleLedPreviewTimer.Start();
            }

            return;
        }

        if (_ruleLedPreviewTimer.IsEnabled)
        {
            _ruleLedPreviewTimer.Stop();
        }

        if (UseLightsCheck.IsChecked != true || !_config.ArduinoEnabled)
        {
            SetRuleLedPreviewAll("#334155");
        }
    }

    private bool ShouldRunRuleLedPreview()
    {
        return UseLightsCheck.IsChecked == true
            && _config.ArduinoEnabled
            && MainTabs.SelectedIndex == 2
            && LightConfigurationPanel.IsExpanded
            && RuleLedPreviewPanel.IsVisible;
    }

    private void UpdateBackgroundLedPreviewFrame()
    {
        if (_initializingComponent || _backgroundLedPreviewDots.Count == 0)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateBackgroundLedPreviewFrame);
            return;
        }

        if (!ShouldRunBackgroundLedPreview())
        {
            UpdateBackgroundLedPreviewTimerState();
            return;
        }

        var pattern = BackgroundPatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Solid;
        var brightness = Math.Clamp(BackgroundBrightnessSlider.Value / 255d, 0d, 1d);
        var colorScale = Math.Clamp(brightness, 0.08, 1d);
        var primary = ParsePreviewColor(BackgroundPrimaryColorBox.Text, "#14B8A6");
        var secondary = ParsePreviewColor(BackgroundSecondaryColorBox.Text, "#B56CFF");
        var tertiary = ParsePreviewColor(BackgroundTertiaryColorBox.Text, "#FFFFFF");
        var count = _backgroundLedPreviewDots.Count;
        _backgroundLedPreviewStep++;

        for (var i = 0; i < count; i++)
        {
            var phase = (i + _backgroundLedPreviewStep) / (double)count;
            var color = pattern switch
            {
                LightPattern.Solid => primary,
                LightPattern.Rainbow => RainbowPreviewColor(phase),
                LightPattern.Pulse => BlendPreviewColor(primary, secondary, (Math.Sin((_backgroundLedPreviewStep * 0.18) + (i * 0.22)) + 1d) / 2d),
                LightPattern.Chase => ((i + _backgroundLedPreviewStep) % 6) < 2
                    ? primary
                    : ScalePreviewColor(secondary, 0.22),
                LightPattern.Theater => ((i + _backgroundLedPreviewStep) % 3) == 0
                    ? primary
                    : (((i + _backgroundLedPreviewStep) % 3) == 1 ? secondary : ScalePreviewColor(tertiary, 0.18)),
                LightPattern.Sparkle => _previewRandom.NextDouble() > 0.72
                    ? RandomPreviewColor(primary, secondary, tertiary)
                    : ScalePreviewColor(primary, 0.16),
                LightPattern.Rave => RandomPreviewColor(primary, secondary, tertiary),
                _ => primary
            };

            _backgroundLedPreviewDots[i] = PreviewDot(ScalePreviewColor(color, colorScale), brightness);
        }
    }

    private void SetBackgroundLedPreviewAll(string color)
    {
        var previewColor = ParsePreviewColor(color, "#334155");
        for (var i = 0; i < _backgroundLedPreviewDots.Count; i++)
        {
            _backgroundLedPreviewDots[i] = PreviewDot(previewColor, 0.08);
        }
    }

    private void UpdateBackgroundLedPreviewTimerState()
    {
        if (_initializingComponent || !Dispatcher.CheckAccess())
        {
            return;
        }

        var shouldRun = ShouldRunBackgroundLedPreview();
        if (shouldRun)
        {
            if (!_backgroundLedPreviewTimer.IsEnabled)
            {
                _backgroundLedPreviewTimer.Start();
            }

            return;
        }

        if (_backgroundLedPreviewTimer.IsEnabled)
        {
            _backgroundLedPreviewTimer.Stop();
        }

        if (BackgroundEnabledCheck.IsChecked != true || !_config.ArduinoEnabled)
        {
            SetBackgroundLedPreviewAll("#334155");
        }
    }

    private bool ShouldRunBackgroundLedPreview()
    {
        return BackgroundEnabledCheck.IsChecked == true
            && _config.ArduinoEnabled
            && MainTabs.SelectedIndex == 3
            && BackgroundLedPreviewPanel.IsVisible;
    }

    private System.Windows.Media.Color RandomPreviewColor(
        System.Windows.Media.Color primary,
        System.Windows.Media.Color secondary,
        System.Windows.Media.Color tertiary)
    {
        return _previewRandom.Next(3) switch
        {
            0 => primary,
            1 => secondary,
            _ => tertiary
        };
    }

    private static System.Windows.Media.Color ParsePreviewColor(string color, string fallback)
    {
        try
        {
            return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(LightCommand.NormalizeColor(color));
        }
        catch
        {
            return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(fallback);
        }
    }

    private static RuleLedPreviewDot PreviewDot(System.Windows.Media.Color color, double brightness)
    {
        var glowOpacity = Math.Clamp(0.12 + (brightness * 0.72), 0.12, 0.9);
        var glowRadius = 7d + (brightness * 22d);
        return new RuleLedPreviewDot(
            FrozenBrushFrom($"#{color.R:X2}{color.G:X2}{color.B:X2}"),
            color,
            glowOpacity,
            glowRadius);
    }

    private static System.Windows.Media.Color ScalePreviewColor(System.Windows.Media.Color color, double factor)
    {
        factor = Math.Clamp(factor, 0d, 1d);
        return System.Windows.Media.Color.FromRgb(
            (byte)Math.Round(color.R * factor),
            (byte)Math.Round(color.G * factor),
            (byte)Math.Round(color.B * factor));
    }

    private static System.Windows.Media.Color BlendPreviewColor(
        System.Windows.Media.Color start,
        System.Windows.Media.Color end,
        double amount)
    {
        amount = Math.Clamp(amount, 0d, 1d);
        return System.Windows.Media.Color.FromRgb(
            (byte)Math.Round(start.R + ((end.R - start.R) * amount)),
            (byte)Math.Round(start.G + ((end.G - start.G) * amount)),
            (byte)Math.Round(start.B + ((end.B - start.B) * amount)));
    }

    private static System.Windows.Media.Color RainbowPreviewColor(double phase)
    {
        phase -= Math.Floor(phase);
        var h = phase * 6d;
        var x = 1d - Math.Abs((h % 2d) - 1d);
        var (r, g, b) = h switch
        {
            < 1d => (1d, x, 0d),
            < 2d => (x, 1d, 0d),
            < 3d => (0d, 1d, x),
            < 4d => (0d, x, 1d),
            < 5d => (x, 0d, 1d),
            _ => (1d, 0d, x)
        };

        return System.Windows.Media.Color.FromRgb(
            (byte)Math.Round(r * 255d),
            (byte)Math.Round(g * 255d),
            (byte)Math.Round(b * 255d));
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
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateStatusText);
            return;
        }

        var channelName = _config.Channel.IsReady
            ? FirstNonEmpty(_config.Channel.DisplayName, _config.Channel.Login, "Canal Twitch")
            : "Sin Twitch";
        var login = _config.Channel.IsReady && !string.IsNullOrWhiteSpace(_config.Channel.Login)
            ? $"@{_config.Channel.Login}"
            : "Sin login";

        ChannelNameText.Text = channelName;
        ChannelLoginText.Text = login;
        TwitchConnectionText.Text = _isTwitchConnecting
            ? "Conectando"
            : !string.IsNullOrWhiteSpace(_twitchConnectionError)
                ? "Revisar conexion"
                : _eventSubClient.IsRunning
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
        ArduinoConnectionText.Text = !_config.ArduinoEnabled
            ? "Desactivado"
            : _isArduinoConnecting
                ? "Conectando"
            : _lightController.HasConfirmedAck
                ? $"Conectado en {_lightController.CurrentPort}"
                : _lightController.HasOpenPort
                    ? "Puerto abierto sin respuesta"
                : "Sin conectar";
        ArduinoStatusText.Text = !_config.ArduinoEnabled
            ? "Las luces Arduino no se mostraran ni ejecutaran."
            : _isArduinoConnecting
                ? $"Intentando conectar con {FirstNonEmpty(_config.SerialPort, "el puerto configurado")}."
            : _lightController.HasConfirmedAck
                ? $"{_config.BaudRate} baudios. {_config.LedStrips.Count} tiras, {totalLeds} LEDs. {activeBackground}."
                : _lightController.HasOpenPort
                    ? "El puerto esta abierto, pero Arduino no ha confirmado ACK."
                : $"Puerto: {FirstNonEmpty(_config.SerialPort, "sin COM")}. {_config.LedStrips.Count} tiras, {totalLeds} LEDs.";
        RefreshDashboardConnectionStates();
        UpdateDashboardSummary();
        UpdateLightsArduinoStatus();

        TwitchButton.Content = _eventSubClient.IsRunning ? "Desconectar Twitch" : "Conectar Twitch";
    }

    private void UpdateLightsArduinoStatus()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateLightsArduinoStatus);
            return;
        }

        if (_initializingComponent)
        {
            return;
        }

        var totalLeds = _config.LedStrips.Sum(strip => strip.LedCount);
        var pins = _config.LedStrips.Count == 0
            ? "Sin pines"
            : string.Join(", ", _config.LedStrips.Select(strip => $"Pin {strip.Pin}"));

        LightsArduinoDeviceText.Text = !_config.ArduinoEnabled
            ? "Desactivado"
            : _lightController.HasConfirmedAck
                ? "Conectado"
                : _lightController.HasOpenPort
                    ? "Sin respuesta"
                    : "Desconectado";
        LightsArduinoPortText.Text = _lightController.HasOpenPort
            ? FirstNonEmpty(_lightController.CurrentPort, _config.SerialPort, "Sin COM")
            : FirstNonEmpty(_config.SerialPort, "Sin COM");
        LightsArduinoLedCountText.Text = totalLeds.ToString();
        LightsArduinoPinsText.Text = pins;
    }

    private void UpdateAlexaStatusText()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateAlexaStatusText);
            return;
        }

        var status = _config.Alexa.IsConfigured
            ? "Alexa lista. Las reglas pueden enviar eventos a la Skill/relay."
            : _config.Alexa.Enabled
                ? "Alexa activa, falta configurar una URL valida de Skill/relay."
                : "Alexa desactivada. Las reglas no mostraran acciones de Alexa.";

        AlexaStatusText.Text = status;
        AlexaConnectionText.Text = _config.Alexa.IsConfigured
            ? _isAlexaConnecting
                ? "Conectando"
                : _alexaRelayConnected
                    ? "Relay conectado"
                    : "Relay configurado"
            : _config.Alexa.Enabled
                ? "Configuracion incompleta"
                : "Desactivado";
        AlexaSidebarStatusText.Text = _config.Alexa.IsConfigured
            ? BuildAlexaSidebarStatusText()
            : status;
        RefreshDashboardConnectionStates();
        UpdateDashboardSummary();
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

    private void UpdateDashboardSummary()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateDashboardSummary);
            return;
        }

        DashboardFollowersSummaryText.Text = $"+{_dashboardFollowersToday}";
        DashboardSubsSummaryText.Text = $"+{_dashboardSubscriptionsToday}";
        DashboardBitsSummaryText.Text = $"+{_dashboardBitsToday}";
        DashboardChatSummaryText.Text = _dashboardChatMessagesToday.ToString();
        DashboardEventsSummaryText.Text = _dashboardEventsToday.ToString();

        DashboardFollowersSummaryText.Foreground = FrozenBrushFrom("#14B8A6");
        DashboardSubsSummaryText.Foreground = FrozenBrushFrom("#B56CFF");
        DashboardBitsSummaryText.Foreground = FrozenBrushFrom("#37C7F3");
        DashboardChatSummaryText.Foreground = FrozenBrushFrom("#22C55E");
        DashboardEventsSummaryText.Foreground = FrozenBrushFrom("#84CC16");

        RefreshDashboardConnectionStates();
    }

    private void RefreshDashboardConnectionStates()
    {
        var twitchState = _isTwitchConnecting
            ? ConnectionVisualState.Connecting
            : !string.IsNullOrWhiteSpace(_twitchConnectionError)
                ? ConnectionVisualState.Warning
                : _config.Token.HasToken
                    ? ConnectionVisualState.Connected
                    : ConnectionVisualState.Disconnected;
        var arduinoState = !_config.ArduinoEnabled
            ? ConnectionVisualState.Disabled
            : _isArduinoConnecting
                ? ConnectionVisualState.Connecting
                : _lightController.HasConfirmedAck
                    ? ConnectionVisualState.Connected
                    : _lightController.HasOpenPort
                        ? ConnectionVisualState.Warning
                        : ConnectionVisualState.Disconnected;
        var alexaState = !_config.Alexa.Enabled
            ? ConnectionVisualState.Disabled
            : _isAlexaConnecting
                ? ConnectionVisualState.Connecting
                : !_config.Alexa.IsConfigured
                    ? ConnectionVisualState.Warning
                    : _alexaRelayConnected
                        ? ConnectionVisualState.Connected
                        : ConnectionVisualState.Warning;
        var audioState = _config.AlertVolumePercent > 0
            ? ConnectionVisualState.Connected
            : ConnectionVisualState.Disabled;

        SetDashboardConnectionState(
            DashboardTwitchStateText,
            DashboardTwitchStatusIcon,
            twitchState,
            warningText: "Revisar");
        SetDashboardConnectionState(
            DashboardArduinoStateText,
            DashboardArduinoStatusIcon,
            arduinoState,
            warningText: "Sin respuesta");
        SetDashboardConnectionState(
            DashboardAlexaStateText,
            DashboardAlexaStatusIcon,
            alexaState,
            warningText: _config.Alexa.IsConfigured ? "Configurado" : "Incompleta");
        SetDashboardConnectionState(
            DashboardAudioStateText,
            DashboardAudioStatusIcon,
            audioState,
            connectedText: $"{_config.AlertVolumePercent}%");

        SetConnectionBadgeState(
            ConnectionsTwitchBadge,
            ConnectionsTwitchBadgeText,
            twitchState,
            warningText: "Revisar");
        SetConnectionBadgeState(
            ConnectionsArduinoBadge,
            ConnectionsArduinoBadgeText,
            arduinoState,
            warningText: "Sin respuesta");
        SetConnectionBadgeState(
            ConnectionsAlexaBadge,
            ConnectionsAlexaBadgeText,
            alexaState,
            warningText: _config.Alexa.IsConfigured ? "Configurado" : "Incompleta");
    }

    private static void SetDashboardConnectionState(
        TextBlock stateText,
        Border statusIcon,
        ConnectionVisualState state,
        string connectedText = "Conectado",
        string disconnectedText = "Desconectado",
        string disabledText = "Desactivado",
        string connectingText = "Conectando",
        string warningText = "Revisar")
    {
        var (text, color, icon) = ConnectionStateVisuals(
            state,
            connectedText,
            disconnectedText,
            disabledText,
            connectingText,
            warningText);
        var brush = FrozenBrushFrom(color);

        stateText.Text = text;
        stateText.Foreground = brush;
        statusIcon.Background = brush;
        statusIcon.OpacityMask = new ImageBrush
        {
            ImageSource = LoadPackImage(icon),
            Stretch = Stretch.Uniform
        };
        statusIcon.ToolTip = text;
    }

    private static void SetConnectionBadgeState(
        Border badge,
        TextBlock textBlock,
        ConnectionVisualState state,
        string connectedText = "Conectado",
        string disconnectedText = "Desconectado",
        string disabledText = "Desactivado",
        string connectingText = "Conectando",
        string warningText = "Revisar")
    {
        var (text, color, _) = ConnectionStateVisuals(
            state,
            connectedText,
            disconnectedText,
            disabledText,
            connectingText,
            warningText);
        var brush = FrozenBrushFrom(color);

        textBlock.Text = text;
        textBlock.Foreground = brush;
        badge.Background = TranslucentBrushFrom(color);
        badge.BorderBrush = brush;
        badge.BorderThickness = new Thickness(1);
    }

    private static (string Text, string Color, string IconPath) ConnectionStateVisuals(
        ConnectionVisualState state,
        string connectedText,
        string disconnectedText,
        string disabledText,
        string connectingText,
        string warningText)
    {
        return state switch
        {
            ConnectionVisualState.Connected => (connectedText, "#22C55E", "Assets/Icons/status_ok.png"),
            ConnectionVisualState.Connecting => (connectingText, "#FFB020", "Assets/Icons/status_warning.png"),
            ConnectionVisualState.Warning => (warningText, "#FFB020", "Assets/Icons/status_warning.png"),
            ConnectionVisualState.Disabled => (disabledText, "#94A3B8", "Assets/Icons/status_empty.png"),
            _ => (disconnectedText, "#F43F5E", "Assets/Icons/status_error.png")
        };
    }

    private static ImageSource? LoadPackImage(string path)
    {
        foreach (var uri in new[]
        {
            $"pack://application:,,,/NeoTwitch;component/{path}",
            $"pack://application:,,,/{path}"
        })
        {
            try
            {
                var image = new BitmapImage(new Uri(uri, UriKind.Absolute));
                image.Freeze();
                return image;
            }
            catch
            {
                // Some WPF resource contexts prefer the assembly-qualified URI, others the app-root URI.
            }
        }

        return null;
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
            TopProfileText.Text = "Perfil";
            TopProfileText.Foreground = palette.Text;
            return;
        }

        TwitchLiveDot.Fill = System.Windows.Media.Brushes.Transparent;
        TwitchLiveDot.Stroke = palette.SidebarText;
        TwitchLiveStateText.Text = "No esta en directo";
        TwitchLiveStateText.Foreground = palette.SidebarText;
        TopProfileText.Text = "Perfil";
        TopProfileText.Foreground = palette.Text;
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

    private void UpdateCloseBehaviorCards()
    {
        if (_initializingComponent)
        {
            return;
        }

        var closeToTray = CloseToTrayCheck.IsChecked == true;
        if (CloseToTrayRadio.IsChecked != closeToTray)
        {
            CloseToTrayRadio.IsChecked = closeToTray;
        }

        if (CloseAppRadio.IsChecked != !closeToTray)
        {
            CloseAppRadio.IsChecked = !closeToTray;
        }

        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;
        ApplyCloseBehaviorCardTheme(CloseToTrayCard, closeToTray, palette);
        ApplyCloseBehaviorCardTheme(CloseAppCard, !closeToTray, palette);
    }

    private static void ApplyCloseBehaviorCardTheme(Border card, bool selected, ThemePalette palette)
    {
        card.Background = selected
            ? TranslucentBrushFrom("#14B8A6")
            : palette.Input;
        card.BorderBrush = selected
            ? palette.Accent
            : palette.Border;
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
        UpdateDashboardSummary();
        UpdateColorButtons();
        UpdateEventKindTileSelection();
        UpdatePatternTileSelection();
        UpdateBackgroundPatternTileSelection();
        UpdateRuleAudioModeSelection();
        UpdateAudioFilterButtons();
        UpdateCloseBehaviorCards();
    }

    private void ApplyThemeToElement(DependencyObject element, ThemePalette palette)
    {
        var skipChildren = false;

        switch (element)
        {
            case Border border when border.TemplatedParent is not null:
                break;
            case Border border when string.Equals(border.Tag?.ToString(), "StaticBrush", StringComparison.OrdinalIgnoreCase):
                break;
            case Border border when border.DataContext is ActivityLogEntry:
                break;
            case Border border:
                border.BorderBrush = palette.Border;
                if (IsSidebarBorder(border))
                {
                    border.Background = palette.Sidebar;
                    break;
                }

                if (IsTitleBarBorder(border))
                {
                    border.Background = palette.Window;
                    border.BorderBrush = palette.Border;
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
            case TextBlock textBlock when string.Equals(textBlock.Tag?.ToString(), "StaticBrush", StringComparison.OrdinalIgnoreCase):
                break;
            case TextBlock textBlock when string.Equals(textBlock.Tag?.ToString(), "Accent", StringComparison.OrdinalIgnoreCase):
                textBlock.Foreground = palette.Accent;
                break;
            case TextBlock textBlock when string.Equals(textBlock.Tag?.ToString(), "Success", StringComparison.OrdinalIgnoreCase):
                textBlock.Foreground = FrozenBrushFrom("#22C55E");
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

                if (IsActivityFeedListBox(listBox))
                {
                    listBox.Background = System.Windows.Media.Brushes.Transparent;
                    listBox.Foreground = palette.Text;
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
            case ToggleButton toggleButton when IsRuleStatusFilterButton(toggleButton):
                ApplyRuleStatusFilterButtonTheme(toggleButton, palette);
                skipChildren = true;
                break;
            case ToggleButton toggleButton:
                ApplyActivityFilterButtonTheme(toggleButton, palette);
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

        if (IsWindowControlButton(button))
        {
            button.Background = System.Windows.Media.Brushes.Transparent;
            button.Foreground = palette.MutedText;
            button.BorderBrush = System.Windows.Media.Brushes.Transparent;
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

    private void ApplyActivityFilterButtonTheme(ToggleButton button, ThemePalette palette)
    {
        var filter = button.Tag?.ToString() ?? "";
        var accentColor = ActivityFilterAccent(filter);
        var accent = FrozenBrushFrom(accentColor);
        var active = button.IsChecked == true;

        button.Background = active
            ? TranslucentBrushFrom(accentColor)
            : palette.Input;
        button.Foreground = active
            ? accent
            : palette.MutedText;
        button.BorderBrush = active
            ? accent
            : palette.Border;
    }

    private void ApplyRuleStatusFilterButtonTheme(ToggleButton button, ThemePalette palette)
    {
        var active = button.IsChecked == true;
        var accentColor = button.Tag?.ToString() switch
        {
            "ACTIVE" => "#22C55E",
            "INACTIVE" => "#94A3B8",
            _ => "#14B8A6"
        };
        var accent = FrozenBrushFrom(accentColor);

        button.Background = active
            ? TranslucentBrushFrom(accentColor)
            : palette.Input;
        button.Foreground = active
            ? accent
            : palette.MutedText;
        button.BorderBrush = active
            ? accent
            : palette.Border;
    }

    private static bool IsRuleStatusFilterButton(ToggleButton button)
    {
        return button.Name.StartsWith("RuleFilter", StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<ToggleButton> RuleStatusFilterButtons()
    {
        return
        [
            RuleFilterAllButton,
            RuleFilterActiveButton,
            RuleFilterInactiveButton
        ];
    }

    private void UpdateEventKindTileSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var selectedKind = EventKindBox.SelectedValue is TwitchEventKind kind
            ? kind
            : TwitchEventKind.Follow;
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        foreach (var button in EventKindTileButtons())
        {
            if (button.Tag is not string value || !Enum.TryParse<TwitchEventKind>(value, out var tileKind))
            {
                continue;
            }

            var selected = tileKind == selectedKind;
            var accentColor = EventKindAccent(tileKind);
            var accent = FrozenBrushFrom(accentColor);
            button.Background = selected
                ? TranslucentBrushFrom(accentColor)
                : palette.Input;
            button.BorderBrush = selected
                ? accent
                : palette.Border;
            button.Foreground = selected
                ? accent
                : palette.Text;
        }
    }

    private IEnumerable<System.Windows.Controls.Button> EventKindTileButtons()
    {
        return
        [
            EventFollowTileButton,
            EventSubscriptionTileButton,
            EventRaidTileButton,
            EventCheerTileButton,
            EventChatCommandTileButton,
            EventRedemptionTileButton
        ];
    }

    private static string EventKindAccent(TwitchEventKind kind)
    {
        return kind switch
        {
            TwitchEventKind.Follow => "#14B8A6",
            TwitchEventKind.Subscription => "#B56CFF",
            TwitchEventKind.Raid => "#F43F5E",
            TwitchEventKind.Cheer => "#37C7F3",
            TwitchEventKind.ChatCommand => "#22C55E",
            TwitchEventKind.ChannelPointRedemption => "#FB923C",
            _ => "#94A3B8"
        };
    }

    private void UpdatePatternTileSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var selectedPattern = PatternBox.SelectedValue is LightPattern pattern
            ? pattern
            : LightPattern.Pulse;
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;
        var tileBackground = _config.DarkMode
            ? palette.Input
            : FrozenBrushFrom("#10202A");
        var tileForeground = _config.DarkMode
            ? palette.Text
            : FrozenBrushFrom("#F8FAFC");

        foreach (var button in PatternTileButtons())
        {
            if (button.Tag is not string value || !Enum.TryParse<LightPattern>(value, out var tilePattern))
            {
                continue;
            }

            var selected = tilePattern == selectedPattern;
            var accentColor = PatternAccent(tilePattern);
            var accent = FrozenBrushFrom(accentColor);
            button.Background = tileBackground;
            button.BorderBrush = selected
                ? accent
                : palette.Border;
            button.Foreground = selected
                ? accent
                : tileForeground;
        }
    }

    private void UpdateRuleAudioModeSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var palette = _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light;
        ApplyRuleAudioModeButtonTheme(RuleSingleAudioModeButton, _ruleAudioMode == AudioSourceMode.Single, "#14B8A6", palette);
        ApplyRuleAudioModeButtonTheme(RuleGroupAudioModeButton, _ruleAudioMode == AudioSourceMode.Group, "#B56CFF", palette);
    }

    private static void ApplyRuleAudioModeButtonTheme(System.Windows.Controls.Button button, bool active, string accentColor, ThemePalette palette)
    {
        button.Background = active ? TranslucentBrushFrom(accentColor) : palette.Input;
        button.Foreground = active ? FrozenBrushFrom(accentColor) : palette.Text;
        button.BorderBrush = active ? FrozenBrushFrom(accentColor) : palette.Border;
    }

    private IEnumerable<System.Windows.Controls.Button> PatternTileButtons()
    {
        return
        [
            PatternSolidTileButton,
            PatternPulseTileButton,
            PatternRainbowTileButton,
            PatternChaseTileButton,
            PatternTheaterTileButton,
            PatternSparkleTileButton,
            PatternRaveTileButton
        ];
    }

    private static string PatternAccent(LightPattern pattern)
    {
        return pattern switch
        {
            LightPattern.Solid => "#14B8A6",
            LightPattern.Pulse => "#B56CFF",
            LightPattern.Rainbow => "#37C7F3",
            LightPattern.Chase => "#22C55E",
            LightPattern.Theater => "#F59E0B",
            LightPattern.Sparkle => "#FACC15",
            LightPattern.Rave => "#EC4899",
            _ => "#94A3B8"
        };
    }

    private void UpdateBackgroundPatternTileSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var selectedPattern = BackgroundPatternBox.SelectedValue is LightPattern pattern
            ? pattern
            : LightPattern.Solid;
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;
        var tileBackground = _config.DarkMode
            ? palette.Input
            : FrozenBrushFrom("#10202A");
        var tileForeground = _config.DarkMode
            ? palette.Text
            : FrozenBrushFrom("#F8FAFC");

        foreach (var button in BackgroundPatternTileButtons())
        {
            if (button.Tag is not string value || !Enum.TryParse<LightPattern>(value, out var tilePattern))
            {
                continue;
            }

            var selected = tilePattern == selectedPattern;
            var accentColor = PatternAccent(tilePattern);
            var accent = FrozenBrushFrom(accentColor);
            button.Background = tileBackground;
            button.BorderBrush = selected
                ? accent
                : palette.Border;
            button.Foreground = selected
                ? accent
                : tileForeground;
        }
    }

    private IEnumerable<System.Windows.Controls.Button> BackgroundPatternTileButtons()
    {
        return
        [
            BackgroundPatternSolidTileButton,
            BackgroundPatternPulseTileButton,
            BackgroundPatternRainbowTileButton,
            BackgroundPatternChaseTileButton,
            BackgroundPatternTheaterTileButton,
            BackgroundPatternSparkleTileButton,
            BackgroundPatternRaveTileButton
        ];
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

        foreach (var button in new[] { NavSettingsButton, NavConnectionsButton, NavRulesButton, NavStripsButton, NavAlexaButton, NavAudioButton, NavPreferencesButton, NavActivityButton })
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

    private IEnumerable<ToggleButton> ActivityFilterButtons()
    {
        return
        [
            ActivityFilterTwitchButton,
            ActivityFilterArduinoButton,
            ActivityFilterAlexaButton,
            ActivityFilterAudioButton,
            ActivityFilterEventButton,
            ActivityFilterSystemButton,
            ActivityFilterImportantButton
        ];
    }

    private static string ActivityFilterAccent(string filter)
    {
        return filter.ToUpperInvariant() switch
        {
            "TWITCH" => "#9146FF",
            "ARDUINO" => "#00878F",
            "ALEXA" => "#2FB4E9",
            "AUDIO" => "#B56CFF",
            "EVENTO" => "#22C55E",
            "SISTEMA" => "#94A3B8",
            "IMPORTANTE" => "#FFB020",
            _ => "#14B8A6"
        };
    }

    private static SolidColorBrush TranslucentBrushFrom(string accentColor)
    {
        return accentColor.StartsWith('#') && accentColor.Length == 7
            ? FrozenBrushFrom($"#22{accentColor[1..]}")
            : FrozenBrushFrom("#2200C7B7");
    }

    private static bool IsActivityFeedListBox(System.Windows.Controls.ListBox listBox)
    {
        return string.Equals(listBox.Name, "ActivityList", StringComparison.OrdinalIgnoreCase)
            || string.Equals(listBox.Name, "DashboardActivityList", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWindowControlButton(System.Windows.Controls.Button button)
    {
        return string.Equals(button.Name, "MinimizeWindowButton", StringComparison.OrdinalIgnoreCase)
            || string.Equals(button.Name, "MaximizeWindowButton", StringComparison.OrdinalIgnoreCase)
            || string.Equals(button.Name, "CloseWindowButton", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSidebarBorder(Border border)
    {
        return string.Equals(border.Name, "SidebarChrome", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTitleBarBorder(Border border)
    {
        return string.Equals(border.Name, "TitleBarChrome", StringComparison.OrdinalIgnoreCase);
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

    private void CustomTitleDragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsInsideButton(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (e.ClickCount >= 2)
        {
            ToggleWindowState();
            return;
        }

        try
        {
            DragMove();
        }
        catch
        {
            // DragMove can throw if the pointer is released before the drag starts.
        }
    }

    private void MinimizeWindowButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeWindowButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleWindowState();
    }

    private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private static bool IsInsideButton(DependencyObject? source)
    {
        for (var current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is System.Windows.Controls.Button)
            {
                return true;
            }
        }

        return false;
    }

    private void SaveConfig()
    {
        try
        {
            _settingsStore.Save(_config);
            if (!_initializingComponent)
            {
                if (Dispatcher.CheckAccess())
                {
                    UpdateDashboardSummary();
                }
                else
                {
                    _ = Dispatcher.BeginInvoke(UpdateDashboardSummary, DispatcherPriority.Background);
                }
            }
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
            var entry = new ActivityLogEntry(message, kind);
            _activity.Insert(0, entry);
            _dashboardActivity.Insert(0, entry);

            while (_activity.Count > 250)
            {
                _activity.RemoveAt(_activity.Count - 1);
            }

            while (_dashboardActivity.Count > 10)
            {
                _dashboardActivity.RemoveAt(_dashboardActivity.Count - 1);
            }
        });
    }

    private static ActivityLogKind ClassifyLogMessage(string message)
    {
        var text = message.ToLowerInvariant();

        if (text.StartsWith("twitch", StringComparison.Ordinal)
            || text.StartsWith("chat", StringComparison.Ordinal)
            || text.Contains("autorizado", StringComparison.Ordinal)
            || text.Contains("escuchando eventos", StringComparison.Ordinal))
        {
            return ActivityLogKind.Twitch;
        }

        if (text.StartsWith("alexa", StringComparison.Ordinal))
        {
            return ActivityLogKind.Alexa;
        }

        if (text.StartsWith("arduino", StringComparison.Ordinal)
            || text.StartsWith("serial", StringComparison.Ordinal)
            || text.StartsWith("fondo", StringComparison.Ordinal)
            || text.StartsWith("luces", StringComparison.Ordinal)
            || text.Contains("puerto com", StringComparison.Ordinal)
            || text.Contains("puertos com", StringComparison.Ordinal))
        {
            return ActivityLogKind.Arduino;
        }

        if (text.StartsWith("audio", StringComparison.Ordinal)
            || text.StartsWith("sonido", StringComparison.Ordinal))
        {
            return ActivityLogKind.Audio;
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

        if (text.Contains("error", StringComparison.Ordinal)
            || text.Contains("fallo", StringComparison.Ordinal)
            || text.Contains("no pude", StringComparison.Ordinal)
            || text.Contains("no puedo", StringComparison.Ordinal)
            || text.Contains("no hay", StringComparison.Ordinal)
            || text.Contains("no encontre", StringComparison.Ordinal))
        {
            return ActivityLogKind.Important;
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
        Arduino,
        Alexa,
        Audio,
        Event,
        Important
    }

    private enum ConnectionVisualState
    {
        Connected,
        Connecting,
        Disconnected,
        Disabled,
        Warning
    }

    private sealed record QueuedAlertSlot(string Id, string RuleId, string RuleName, TwitchEventKind EventKind);

    private sealed class ActivityLogEntry
    {
        public ActivityLogEntry(string message, ActivityLogKind kind)
        {
            Kind = kind;
            Time = DateTime.Now.ToString("HH:mm");
            Message = message;
            SourceKey = ChooseSourceKey(message, kind);
            FilterKey = SourceKey;
            SourceName = SourceDisplayName(SourceKey);
            Category = BuildCategory(message, kind);
            Title = BuildTitle(message, kind);
            Description = BuildDescription(message, Title);
            var accentColor = ChooseAccentColor(message, kind);
            var sourceAccentColor = ActivityFilterAccent(SourceKey);
            SourceBrush = FrozenBrushFrom(sourceAccentColor);
            SourceBackgroundBrush = TranslucentBrushFrom(sourceAccentColor);
            SourceIconImageSource = LoadActivityIcon(ChooseServiceIconPath(SourceKey));
            SourceImageVisibility = SourceIconImageSource is null
                ? Visibility.Collapsed
                : Visibility.Visible;
            SourceVectorVisibility = SourceIconImageSource is null
                ? Visibility.Visible
                : Visibility.Collapsed;
            SourceIconGeometry = Geometry.Parse(IconData(ChooseSourceIconKey(SourceKey)));
            StatusText = ChooseStatusText(message, kind);
            IsImportant = kind == ActivityLogKind.Important || !string.Equals(StatusText, "OK", StringComparison.OrdinalIgnoreCase);
            var statusAccentColor = StatusAccent(StatusText);
            StatusBrush = FrozenBrushFrom(statusAccentColor);
            StatusBackgroundBrush = TranslucentBrushFrom(statusAccentColor);
            StatusIconImageSource = LoadActivityIcon(ChooseStatusIconPath(StatusText, FilterKey));

            AccentBrush = FrozenBrushFrom(accentColor);
            IconBackgroundBrush = BackgroundBrushFrom(accentColor);
            var activityIconPath = ChooseActivityIconPath(message, kind, SourceKey);
            IconImageSource = LoadActivityIcon(activityIconPath);
            ImageIconVisibility = IconImageSource is null
                ? Visibility.Collapsed
                : Visibility.Visible;
            OriginalImageIconVisibility = IsServiceIconPath(activityIconPath) && IconImageSource is not null
                ? Visibility.Visible
                : Visibility.Collapsed;
            TintedImageIconVisibility = !IsServiceIconPath(activityIconPath) && IconImageSource is not null
                ? Visibility.Visible
                : Visibility.Collapsed;
            VectorIconVisibility = IconImageSource is null
                ? Visibility.Visible
                : Visibility.Collapsed;
            IconGeometry = Geometry.Parse(IconData(ChooseIconKey(message, kind)));
        }

        public ActivityLogKind Kind { get; }
        public string Time { get; }
        public string Message { get; }
        public string SourceKey { get; }
        public string FilterKey { get; }
        public bool IsImportant { get; }
        public string SourceName { get; }
        public string Category { get; }
        public string Title { get; }
        public string Description { get; }
        public Geometry IconGeometry { get; }
        public ImageSource? IconImageSource { get; }
        public Visibility ImageIconVisibility { get; }
        public Visibility OriginalImageIconVisibility { get; }
        public Visibility TintedImageIconVisibility { get; }
        public Visibility VectorIconVisibility { get; }
        public SolidColorBrush AccentBrush { get; }
        public SolidColorBrush IconBackgroundBrush { get; }
        public Geometry SourceIconGeometry { get; }
        public ImageSource? SourceIconImageSource { get; }
        public Visibility SourceImageVisibility { get; }
        public Visibility SourceVectorVisibility { get; }
        public SolidColorBrush SourceBrush { get; }
        public SolidColorBrush SourceBackgroundBrush { get; }
        public string StatusText { get; }
        public ImageSource? StatusIconImageSource { get; }
        public SolidColorBrush StatusBrush { get; }
        public SolidColorBrush StatusBackgroundBrush { get; }

        public bool MatchesFilter(IReadOnlySet<string> enabledFilters, string searchText)
        {
            var sourceEnabled = enabledFilters.Contains(FilterKey);
            var importantEnabled = enabledFilters.Contains("IMPORTANTE");
            var hasAnySourceEnabled = enabledFilters.Any(filter => !string.Equals(filter, "IMPORTANTE", StringComparison.OrdinalIgnoreCase));

            if (IsImportant)
            {
                if (!importantEnabled)
                {
                    return false;
                }

                if (hasAnySourceEnabled && !sourceEnabled)
                {
                    return false;
                }
            }
            else if (!sourceEnabled)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            return Message.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || SourceName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || Category.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || StatusText.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        }

        private static string ChooseSourceKey(string message, ActivityLogKind kind)
        {
            var text = message.ToLowerInvariant();

            if (kind == ActivityLogKind.Event)
            {
                return "EVENTO";
            }

            if (IsTwitchMessage(text, kind)
                || text.StartsWith("chat", StringComparison.Ordinal)
                || text.Contains("autorizado", StringComparison.Ordinal)
                || text.Contains("escuchando eventos", StringComparison.Ordinal))
            {
                return "TWITCH";
            }

            if (IsArduinoMessage(text, kind)
                || text.StartsWith("fondo", StringComparison.Ordinal)
                || text.StartsWith("luces", StringComparison.Ordinal))
            {
                return "ARDUINO";
            }

            if (IsAlexaMessage(text, kind))
            {
                return "ALEXA";
            }

            if (IsAudioMessage(text, kind))
            {
                return "AUDIO";
            }

            return "SISTEMA";
        }

        private static string SourceDisplayName(string sourceKey)
        {
            return sourceKey switch
            {
                "TWITCH" => "Twitch",
                "ARDUINO" => "Arduino",
                "ALEXA" => "Alexa",
                "AUDIO" => "Audio",
                "EVENTO" => "Evento",
                "IMPORTANTE" => "Importante",
                _ => "Sistema"
            };
        }

        private static string BuildCategory(string message, ActivityLogKind kind)
        {
            var text = message.ToLowerInvariant();

            if (kind == ActivityLogKind.Event)
            {
                if (text.Contains("bits", StringComparison.Ordinal))
                {
                    return "BITS";
                }

                if (text.Contains("suscripcion", StringComparison.Ordinal) || text.Contains("suscribio", StringComparison.Ordinal))
                {
                    return "SUB";
                }

                if (text.Contains("siguio", StringComparison.Ordinal) || text.Contains("seguidor", StringComparison.Ordinal))
                {
                    return "SEGUIDOR";
                }

                if (text.Contains("chat", StringComparison.Ordinal) || text.Contains("comando", StringComparison.Ordinal))
                {
                    return "CHAT";
                }

                if (text.Contains("raid", StringComparison.Ordinal))
                {
                    return "RAID";
                }

                if (text.Contains("canje", StringComparison.Ordinal))
                {
                    return "CANJE";
                }

                return "EVENTO";
            }

            if (IsTwitchMessage(text, kind))
            {
                return "TWITCH";
            }

            if (IsArduinoMessage(text, kind))
            {
                return "ARDUINO";
            }

            if (IsAlexaMessage(text, kind))
            {
                return "ALEXA";
            }

            if (IsAudioMessage(text, kind))
            {
                return "AUDIO";
            }

            return kind == ActivityLogKind.Important ? "IMPORTANTE" : "SISTEMA";
        }

        private static string BuildTitle(string message, ActivityLogKind kind)
        {
            var text = message.ToLowerInvariant();

            if (kind == ActivityLogKind.Event)
            {
                if (text.Contains("bits", StringComparison.Ordinal))
                {
                    return "Bits recibidos";
                }

                if (text.Contains("suscripcion", StringComparison.Ordinal) || text.Contains("suscribio", StringComparison.Ordinal))
                {
                    return "Suscripcion";
                }

                if (text.Contains("raid", StringComparison.Ordinal))
                {
                    return "Raid recibida";
                }

                if (text.Contains("siguio", StringComparison.Ordinal) || text.Contains("seguidor", StringComparison.Ordinal))
                {
                    return "Nuevo seguidor";
                }

                if (text.Contains("canje", StringComparison.Ordinal))
                {
                    return "Canje activado";
                }

                if (text.Contains("comando", StringComparison.Ordinal) || text.Contains("chat", StringComparison.Ordinal))
                {
                    return "Comando de chat";
                }

                if (text.Contains("prueba", StringComparison.Ordinal))
                {
                    return "Prueba de alerta";
                }

                return "Alerta activada";
            }

            if (IsTwitchMessage(text, kind))
            {
                return kind == ActivityLogKind.Important ? "Aviso de Twitch" : "Twitch";
            }

            if (IsArduinoMessage(text, kind))
            {
                return kind == ActivityLogKind.Important ? "Aviso de Arduino" : "Arduino";
            }

            if (IsAlexaMessage(text, kind))
            {
                return text.Contains("fondo", StringComparison.Ordinal) ? "Rutina Alexa" : kind == ActivityLogKind.Important ? "Aviso de Alexa" : "Alexa";
            }

            if (IsAudioMessage(text, kind))
            {
                return kind == ActivityLogKind.Important ? "Aviso de audio" : "Audio";
            }

            if (kind == ActivityLogKind.Important)
            {
                return "Aviso importante";
            }

            if (text.StartsWith("fondo", StringComparison.Ordinal) || text.StartsWith("luces", StringComparison.Ordinal))
            {
                return "Luces";
            }

            if (text.StartsWith("configuracion", StringComparison.Ordinal))
            {
                return "Configuracion";
            }

            if (text.StartsWith("version", StringComparison.Ordinal))
            {
                return "Version";
            }

            if (text.StartsWith("simulador", StringComparison.Ordinal))
            {
                return "Simulador";
            }

            return "Sistema";
        }

        private static string BuildDescription(string message, string title)
        {
            var clean = message.Trim();
            var separator = clean.IndexOf(':', StringComparison.Ordinal);
            if (separator > 0 && separator < clean.Length - 1)
            {
                var prefix = clean[..separator].Trim();
                if (string.Equals(prefix, title, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(prefix, "Twitch", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(prefix, "Alexa", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(prefix, "Arduino", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(prefix, "Audio", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(prefix, "Chat", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(prefix, "Fondo", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(prefix, "Luces", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(prefix, "Version", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(prefix, "Configuracion", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(prefix, "Simulador", StringComparison.OrdinalIgnoreCase))
                {
                    clean = clean[(separator + 1)..].Trim();
                }
            }

            return string.IsNullOrWhiteSpace(clean)
                ? message
                : clean;
        }

        private static string ChooseAccentColor(string message, ActivityLogKind kind)
        {
            var text = message.ToLowerInvariant();

            if (kind == ActivityLogKind.Event)
            {
                if (text.Contains("bits", StringComparison.Ordinal))
                {
                    return "#37C7F3";
                }

                if (text.Contains("suscripcion", StringComparison.Ordinal) || text.Contains("suscribio", StringComparison.Ordinal))
                {
                    return "#B56CFF";
                }

                if (text.Contains("siguio", StringComparison.Ordinal) || text.Contains("seguidor", StringComparison.Ordinal))
                {
                    return "#14B8A6";
                }

                if (text.Contains("chat", StringComparison.Ordinal) || text.Contains("comando", StringComparison.Ordinal))
                {
                    return "#22C55E";
                }

                if (text.Contains("raid", StringComparison.Ordinal))
                {
                    return "#F59E0B";
                }

                return "#00C7B7";
            }

            if (IsTwitchMessage(text, kind))
            {
                return "#9146FF";
            }

            if (IsArduinoMessage(text, kind))
            {
                return "#00878F";
            }

            if (IsAlexaMessage(text, kind))
            {
                return "#2FB4E9";
            }

            if (IsAudioMessage(text, kind))
            {
                return "#B56CFF";
            }

            return kind == ActivityLogKind.Important ? "#FFB020" : "#AFA4CC";
        }

        private static string ChooseServiceIconPath(string sourceKey)
        {
            return sourceKey switch
            {
                "TWITCH" => "Assets/Icons/service_twitch.png",
                "ARDUINO" => "Assets/Icons/service_arduino.png",
                "ALEXA" => "Assets/Icons/service_alexa.png",
                "AUDIO" => "Assets/Icons/service_audio.png",
                _ => ""
            };
        }

        private static string ChooseActivityIconPath(string message, ActivityLogKind kind, string sourceKey)
        {
            var text = message.ToLowerInvariant();

            if (kind == ActivityLogKind.Event)
            {
                if (text.Contains("bits", StringComparison.Ordinal))
                {
                    return "Assets/Icons/action_bits.png";
                }

                if (text.Contains("suscripcion", StringComparison.Ordinal) || text.Contains("suscribio", StringComparison.Ordinal))
                {
                    return "Assets/Icons/action_subscription.png";
                }

                if (text.Contains("siguio", StringComparison.Ordinal) || text.Contains("seguidor", StringComparison.Ordinal))
                {
                    return "Assets/Icons/action_follower.png";
                }

                if (text.Contains("chat", StringComparison.Ordinal) || text.Contains("comando", StringComparison.Ordinal))
                {
                    return "Assets/Icons/action_message.png";
                }

                return "Assets/Icons/activity_notification.png";
            }

            if (kind == ActivityLogKind.Important)
            {
                return "Assets/Icons/status_important.png";
            }

            return ChooseServiceIconPath(sourceKey);
        }

        private static bool IsServiceIconPath(string iconPath)
        {
            return iconPath.Contains("/service_", StringComparison.OrdinalIgnoreCase)
                || iconPath.Contains("\\service_", StringComparison.OrdinalIgnoreCase);
        }

        private static string ChooseSourceIconKey(string sourceKey)
        {
            return sourceKey switch
            {
                "EVENTO" => "Event",
                "IMPORTANTE" => "Warning",
                "SISTEMA" => "Settings",
                _ => "Activity"
            };
        }

        private static string ChooseStatusText(string message, ActivityLogKind kind)
        {
            var text = message.ToLowerInvariant();
            if (text.Contains("error", StringComparison.Ordinal)
                || text.Contains("fallo", StringComparison.Ordinal)
                || text.Contains("no pude", StringComparison.Ordinal)
                || text.Contains("no puedo", StringComparison.Ordinal)
                || text.Contains("no hay", StringComparison.Ordinal)
                || text.Contains("no encontre", StringComparison.Ordinal)
                || text.Contains("no se pudo", StringComparison.Ordinal)
                || text.Contains("tardo demasiado", StringComparison.Ordinal))
            {
                return "Error";
            }

            if (kind == ActivityLogKind.Important
                || text.Contains("advertencia", StringComparison.Ordinal)
                || text.Contains("aviso", StringComparison.Ordinal)
                || text.Contains("descart", StringComparison.Ordinal)
                || text.Contains("no coincide", StringComparison.Ordinal))
            {
                return "Aviso";
            }

            return "OK";
        }

        private static string StatusAccent(string statusText)
        {
            return statusText switch
            {
                "Error" => "#F43F5E",
                "Aviso" => "#FFB020",
                _ => "#22C55E"
            };
        }

        private static string ChooseStatusIconPath(string statusText, string filterKey)
        {
            return statusText switch
            {
                "Error" => "Assets/Icons/status_error.png",
                "Aviso" when string.Equals(filterKey, "IMPORTANTE", StringComparison.OrdinalIgnoreCase) => "Assets/Icons/status_important.png",
                "Aviso" => "Assets/Icons/status_warning.png",
                _ => "Assets/Icons/status_ok.png"
            };
        }

        private static string ChooseIconKey(string message, ActivityLogKind kind)
        {
            var text = message.ToLowerInvariant();

            if (kind == ActivityLogKind.Important)
            {
                return "Warning";
            }

            if (kind == ActivityLogKind.Event)
            {
                if (text.Contains("bits", StringComparison.Ordinal))
                {
                    return "Bits";
                }

                if (text.Contains("suscripcion", StringComparison.Ordinal) || text.Contains("suscribio", StringComparison.Ordinal))
                {
                    return "Star";
                }

                if (text.Contains("siguio", StringComparison.Ordinal) || text.Contains("seguidor", StringComparison.Ordinal))
                {
                    return "Users";
                }

                if (text.Contains("chat", StringComparison.Ordinal) || text.Contains("comando", StringComparison.Ordinal))
                {
                    return "Chat";
                }

                if (text.Contains("raid", StringComparison.Ordinal))
                {
                    return "Zap";
                }

                return "Event";
            }

            if (text.StartsWith("arduino", StringComparison.Ordinal))
            {
                return "Arduino";
            }

            if (text.StartsWith("fondo", StringComparison.Ordinal) || text.StartsWith("luces", StringComparison.Ordinal))
            {
                return "Sun";
            }

            return "Activity";
        }

        private static bool IsTwitchMessage(string text, ActivityLogKind kind)
        {
            return kind == ActivityLogKind.Twitch || text.StartsWith("twitch", StringComparison.Ordinal);
        }

        private static bool IsArduinoMessage(string text, ActivityLogKind kind)
        {
            return kind == ActivityLogKind.Arduino
                || text.StartsWith("arduino", StringComparison.Ordinal)
                || text.StartsWith("serial", StringComparison.Ordinal);
        }

        private static bool IsAlexaMessage(string text, ActivityLogKind kind)
        {
            return kind == ActivityLogKind.Alexa || text.StartsWith("alexa", StringComparison.Ordinal);
        }

        private static bool IsAudioMessage(string text, ActivityLogKind kind)
        {
            return kind == ActivityLogKind.Audio
                || text.Contains("audio", StringComparison.Ordinal)
                || text.Contains("sonido", StringComparison.Ordinal);
        }

        private static SolidColorBrush BackgroundBrushFrom(string accentColor)
        {
            return accentColor.StartsWith('#') && accentColor.Length == 7
                ? FrozenBrushFrom($"#22{accentColor[1..]}")
                : FrozenBrushFrom("#2200C7B7");
        }

        private static ImageSource? LoadActivityIcon(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            foreach (var uri in new[]
            {
                $"pack://application:,,,/NeoTwitch;component/{path}",
                $"pack://application:,,,/{path}"
            })
            {
                try
                {
                    var image = new BitmapImage(new Uri(uri, UriKind.Absolute));
                    image.Freeze();
                    return image;
                }
                catch
                {
                    // Try the next pack URI format.
                }
            }

            return null;
        }
    }

    private static SolidColorBrush FrozenBrushFrom(string hex)
    {
        var brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    private sealed record RuleLedPreviewDot(
        SolidColorBrush Fill,
        System.Windows.Media.Color GlowColor,
        double GlowOpacity,
        double GlowRadius);

    private sealed record AudioLibraryRow(
        string Id,
        string Name,
        string FilePath,
        string GroupId,
        string AssignedAlertText,
        string GroupName,
        string DurationText,
        bool HasAssignedAlert,
        bool IsPreviewing,
        SolidColorBrush AssignedAlertBrush,
        SolidColorBrush AssignedAlertBackground,
        int Index)
    {
        public Visibility AssignedAlertVisibility => HasAssignedAlert ? Visibility.Visible : Visibility.Collapsed;

        public Visibility PlayIconVisibility => IsPreviewing ? Visibility.Collapsed : Visibility.Visible;

        public Visibility PauseIconVisibility => IsPreviewing ? Visibility.Visible : Visibility.Collapsed;

        public string PlayToolTip => IsPreviewing ? "Detener audio" : "Reproducir audio";
    }

    private sealed record AudioGroupRow(
        string Id,
        string Name,
        string CountText,
        SolidColorBrush AccentBrush);

    public sealed record AudioGroupChoice(string Id, string Name)
    {
        public override string ToString() => Name;
    }

    public sealed record AudioAlertChoice(string Id, string Name)
    {
        public override string ToString() => Name;
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
