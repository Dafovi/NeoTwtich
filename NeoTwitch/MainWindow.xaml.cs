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
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;
using NeoTwitch.ViewModels.Obs;
using NeoTwitch.ViewModels.Status;
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
    private readonly ObsWebSocketService _obsService = new();
    private readonly VersionCheckService _versionCheckService = new();
    private readonly IUiTextService _text = UiTextService.CreateDefault();
    private readonly TwitchEventSubClient _eventSubClient;
    private readonly ObservableCollection<ActivityLogEntry> _activity = [];
    private readonly ObservableCollection<ActivityLogEntry> _dashboardActivity = [];
    private readonly ObservableCollection<AudioLibraryRow> _audioLibraryRows = [];
    private readonly ObservableCollection<AudioGroupRow> _audioGroupRows = [];
    private readonly ObservableCollection<MediaLibraryRow> _imageLibraryRows = [];
    private readonly ObservableCollection<MediaGroupRow> _imageGroupRows = [];
    private readonly ObservableCollection<MediaLibraryRow> _videoLibraryRows = [];
    private readonly ObservableCollection<MediaGroupRow> _videoGroupRows = [];
    private readonly ObservableCollection<ObsSceneRow> _obsSceneRows = [];
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
        "OBS",
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
    private bool _showObsPassword;
    private bool _alexaRelayConnected;
    private bool _isObsConnecting;
    private bool _isTwitchAuthorizing;
    private bool _isTwitchConnecting;
    private bool _isArduinoConnecting;
    private bool _isAlexaConnecting;
    private bool _isCustomMaximized;
    private bool? _lastAppliedStartWithWindows;
    private Rect _restoreWindowBounds = Rect.Empty;
    private string _twitchConnectionError = "";
    private string _obsConnectionError = "";
    private string _activitySearchText = "";
    private string _ruleSearchText = "";
    private string _ruleStatusFilter = "ALL";
    private string _ruleCategoryFilter = "";
    private string _audioSearchText = "";
    private string _audioFilter = "ALL";
    private string _audioGroupFilterId = "";
    private string _newAudioPath = "";
    private string _imageSearchText = "";
    private string _imageFilter = "ALL";
    private string _imageGroupFilterId = "";
    private string _newImagePath = "";
    private string _videoSearchText = "";
    private string _videoFilter = "ALL";
    private string _videoGroupFilterId = "";
    private string _newVideoPath = "";
    private AudioSourceMode _ruleAudioMode = AudioSourceMode.Single;
    private bool _refreshingAudioLibrary;
    private bool _refreshingImageLibrary;
    private bool _refreshingVideoLibrary;
    private string _audioGroupChoicesSignature = "";
    private string _audioAlertChoicesSignature = "";
    private string _imageGroupChoicesSignature = "";
    private string _videoGroupChoicesSignature = "";
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

    public ObservableCollection<MediaGroupChoice> ImageGroupChoices { get; } = [];

    public ObservableCollection<MediaGroupChoice> VideoGroupChoices { get; } = [];

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
            ImageLibraryList.ItemsSource = _imageLibraryRows;
            ImageGroupsList.ItemsSource = _imageGroupRows;
            VideoLibraryList.ItemsSource = _videoLibraryRows;
            VideoGroupsList.ItemsSource = _videoGroupRows;
            ObsScenesList.ItemsSource = _obsSceneRows;
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
            RuleObsSceneBox.ItemsSource = _obsSceneRows;
            RuleObsSceneBox.DisplayMemberPath = nameof(ObsSceneRow.Name);
            RuleObsSceneBox.SelectedValuePath = nameof(ObsSceneRow.Name);
            NewAudioAlertBox.ItemsSource = AudioAlertChoices;
            NewAudioAlertBox.DisplayMemberPath = nameof(AudioAlertChoice.Name);
            NewAudioAlertBox.SelectedValuePath = nameof(AudioAlertChoice.Id);
            NewAudioGroupBox.ItemsSource = AudioGroupChoices;
            NewAudioGroupBox.DisplayMemberPath = nameof(AudioGroupChoice.Name);
            NewAudioGroupBox.SelectedValuePath = nameof(AudioGroupChoice.Id);
            NewImageGroupBox.ItemsSource = ImageGroupChoices;
            NewImageGroupBox.DisplayMemberPath = nameof(MediaGroupChoice.Name);
            NewImageGroupBox.SelectedValuePath = nameof(MediaGroupChoice.Id);
            NewVideoGroupBox.ItemsSource = VideoGroupChoices;
            NewVideoGroupBox.DisplayMemberPath = nameof(MediaGroupChoice.Name);
            NewVideoGroupBox.SelectedValuePath = nameof(MediaGroupChoice.Id);
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
        NavImagesButton.Content = CreateNavigationItem("Assets/Icons/nav_images.png", "Imagenes");
        NavVideosButton.Content = CreateNavigationItem("Assets/Icons/nav_videos.png", "Videos");
        NavObsButton.Content = CreateNavigationItem("Assets/Icons/nav_obs.png", "OBS");
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
            ["Probar OBS"] = "Play",
            ["Conectar OBS"] = "Plug",
            ["Desconectar OBS"] = "Plug",
            ["Actualizar escenas"] = "Refresh",
            ["Cambiar ahora"] = "Play",
            ["Ver guia OBS"] = "Book",
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
            "Refresh" => "M20,7 L20,13 L14,13 M4,17 L4,11 L10,11 M6,9 C7.2,5.5 10.5,3.5 14.2,4.2 C16.5,4.6 18.4,6 19.5,8 M17.8,15 C16.6,18.4 13.2,20.4 9.5,19.7 C7.2,19.3 5.3,18 4.2,16",
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

        if (_config.Obs.Enabled && _config.Obs.AutoReconnect)
        {
            try
            {
                await ConnectObsAsync();
            }
            catch (Exception ex)
            {
                AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            }
        }
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
        RefreshRulesView();
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
        RefreshRulesView();
        UpdateRuleOptionVisibility();
    }

    private async void TestAlexaButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isAlexaConnecting)
        {
            return;
        }

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

    private void ToggleObsPasswordVisibility_Click(object sender, RoutedEventArgs e)
    {
        _showObsPassword = !_showObsPassword;
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
        if (int.TryParse(NavActivityButton.Tag?.ToString(), out var activityTabIndex))
        {
            MainTabs.SelectedIndex = activityTabIndex;
        }

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
        UpdateRuleExternalActionAvailability();
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

    private void UpdateRuleExternalActionAvailability()
    {
        if (_config.Rules.Count == 0)
        {
            return;
        }

        var lightsAvailable = _config.ArduinoEnabled;
        var alexaAvailable = _config.Alexa.IsConfigured;
        var obsAvailable = _config.Obs.IsConfigured;

        foreach (var rule in _config.Rules)
        {
            rule.LightsActionAvailable = lightsAvailable;
            rule.AlexaActionAvailable = alexaAvailable;
            rule.ObsActionAvailable = obsAvailable;
        }
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
        ObsSceneRestoreRequest? obsRestore = null;

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

            obsRestore = await SendRuleObsSceneAsync(rule, effectCts.Token);

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

            await RestoreRuleObsSceneAsync(obsRestore, wasCancelled);

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
            ObsEnabledCheck.IsChecked = _config.Obs.Enabled;
            ObsHostBox.Text = _config.Obs.Host;
            ObsPortBox.Text = _config.Obs.Port.ToString();
            ObsPasswordBox.Text = _config.Obs.Password;
            ObsAutoReconnectCheck.IsChecked = _config.Obs.AutoReconnect;
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
            UpdateObsStatusText();
            UpdateSensitiveFieldVisibility();
            ApplyTheme();
            UpdateStatusText();
            RefreshMediaLibraryView(MediaLibraryKind.Image);
            RefreshMediaLibraryView(MediaLibraryKind.Video);
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
            ObsSceneCheck.IsChecked = rule.SendObsScene;
            RuleObsSceneBox.SelectedValue = rule.ObsSceneName;
            ObsReturnCheck.IsChecked = rule.ObsReturnToPreviousScene;
            ObsReturnDelayBox.Text = rule.ObsReturnDelayMs.ToString();
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
        _config.Obs.Enabled = ObsEnabledCheck.IsChecked == true;
        _config.Obs.Host = string.IsNullOrWhiteSpace(ObsHostBox.Text) ? "127.0.0.1" : ObsHostBox.Text.Trim();
        _config.Obs.Port = ParseInt(ObsPortBox.Text, 4455, 1, 65535);
        _config.Obs.Password = ObsPasswordBox.Text;
        _config.Obs.AutoReconnect = ObsAutoReconnectCheck.IsChecked == true;
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
        rule.SendObsScene = ObsSceneCheck.IsChecked == true;
        rule.ObsSceneName = RuleObsSceneBox.SelectedValue as string ?? RuleObsSceneBox.Text.Trim();
        rule.ObsReturnToPreviousScene = ObsReturnCheck.IsChecked == true;
        rule.ObsReturnDelayMs = ParseInt(ObsReturnDelayBox.Text, 15000, 0, 600000);
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
        var obsAvailable = _config.Obs.IsConfigured;
        var sendObs = ObsSceneCheck.IsChecked == true;
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
        SetVisible(obsAvailable, ObsActionCard);
        SetVisible(obsAvailable && sendObs, ObsDetailsPanel);
        SetVisible(obsAvailable && sendObs && _obsSceneRows.Count == 0, RuleObsEmptyHintText);

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

    private void UpdateConnectionButtons()
    {
        var twitchBusy = _isTwitchAuthorizing || _isTwitchConnecting;
        TwitchButton.IsEnabled = !twitchBusy;
        TwitchButton.Content = _isTwitchAuthorizing
            ? "Autorizando..."
            : _isTwitchConnecting
                ? "Conectando..."
                : _eventSubClient.IsRunning
                    ? "Desconectar Twitch"
                    : "Conectar Twitch";

        ConnectArduinoButton.IsEnabled = !_isArduinoConnecting && _config.ArduinoEnabled;
        ConnectArduinoButton.Content = _isArduinoConnecting
            ? "Conectando..."
            : "Conectar Arduino";

        TestAlexaButton.IsEnabled = !_isAlexaConnecting && _config.Alexa.Enabled;
        TestAlexaButton.Content = _isAlexaConnecting
            ? "Probando..."
            : "Probar Alexa";

        ConnectObsButton.IsEnabled = !_isObsConnecting && _config.Obs.Enabled;
        ConnectObsButton.Content = _isObsConnecting
            ? "Conectando..."
            : _obsService.IsConnected
                ? "Desconectar OBS"
                : "Conectar OBS";
        ConnectObsButtonPanel.IsEnabled = ConnectObsButton.IsEnabled;
        ConnectObsButtonPanel.Content = ConnectObsButton.Content;

        TestObsButton.IsEnabled = !_isObsConnecting && _config.Obs.Enabled;
        TestObsButton.Content = _isObsConnecting
            ? "Actualizando..."
            : "Actualizar escenas";
        TestObsButtonPanel.IsEnabled = TestObsButton.IsEnabled;
        TestObsButtonPanel.Content = TestObsButton.Content;
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
        UpdateSensitiveField(ObsPasswordBox, ObsPasswordMaskText, ObsPasswordRevealButton, _showObsPassword);
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

        if (_isCustomMaximized)
        {
            RestoreWindowFromWorkArea();
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
        if (_isCustomMaximized)
        {
            RestoreWindowFromWorkArea();
            return;
        }

        MaximizeWindowToWorkArea();
    }

    private void MaximizeWindowToWorkArea()
    {
        _restoreWindowBounds = new Rect(Left, Top, Width, Height);

        var handle = new WindowInteropHelper(this).Handle;
        var area = Forms.Screen.FromHandle(handle).WorkingArea;
        var source = PresentationSource.FromVisual(this);
        var transform = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var topLeft = transform.Transform(new System.Windows.Point(area.Left, area.Top));
        var bottomRight = transform.Transform(new System.Windows.Point(area.Right, area.Bottom));

        WindowState = WindowState.Normal;
        Left = topLeft.X;
        Top = topLeft.Y;
        Width = Math.Max(MinWidth, bottomRight.X - topLeft.X);
        Height = Math.Max(MinHeight, bottomRight.Y - topLeft.Y);
        _isCustomMaximized = true;
    }

    private void RestoreWindowFromWorkArea()
    {
        WindowState = WindowState.Normal;
        _isCustomMaximized = false;

        if (_restoreWindowBounds.IsEmpty)
        {
            return;
        }

        Left = _restoreWindowBounds.Left;
        Top = _restoreWindowBounds.Top;
        Width = _restoreWindowBounds.Width;
        Height = _restoreWindowBounds.Height;
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

    private sealed record QueuedAlertSlot(string Id, string RuleId, string RuleName, TwitchEventKind EventKind);

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
