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
using NeoTwitch.Services.Alerts;
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
    private readonly ObsOverlayService _obsOverlayService = new();
    private readonly AppUpdateService _updateService = new();
    private readonly IUiTextService _text = UiTextService.CreateDefault();
    private readonly AppStartupOptions _startupOptions;
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
    private readonly ObservableCollection<ObsSceneChoice> _obsSceneChoices = [];
    private readonly ObservableCollection<RuleLedPreviewDot> _ruleLedPreviewDots = [];
    private readonly ObservableCollection<RuleLedPreviewDot> _backgroundLedPreviewDots = [];
    private readonly CollectionViewSource _activityViewSource = new();
    private readonly CollectionViewSource _rulesViewSource = new();
    private readonly DispatcherTimer _ruleLedPreviewTimer = new();
    private readonly DispatcherTimer _backgroundLedPreviewTimer = new();
    private readonly DispatcherTimer _arduinoMonitorTimer = new();
    private readonly Random _previewRandom = new();
    private readonly Random _audioRandom = new();
    private readonly SemaphoreSlim _effectGate = new(1, 1);
    private readonly AlertQueueService _alertQueue = new();
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
    private readonly UiOption<ObsMediaKind>[] _obsMediaKindOptions =
    [
        new("Imagen", ObsMediaKind.Image),
        new("Video", ObsMediaKind.Video)
    ];
    private readonly UiOption<MediaSourceMode>[] _mediaSourceModeOptions =
    [
        new("Un archivo", MediaSourceMode.Single),
        new("Grupo aleatorio", MediaSourceMode.Group)
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
    private bool _isObsSceneActionRunning;
    private bool _hasUnsavedRuleChanges;
    private bool _suppressRuleSelectionChange;
    private bool _updatingLightValueFields;
    private bool _arduinoMonitorBusy;
    private bool _lastArduinoPortPresent = true;
    private bool? _lastAppliedStartWithWindows;
    private Rect _restoreWindowBounds = Rect.Empty;
    private DateTimeOffset _lastArduinoReconnectAttempt = DateTimeOffset.MinValue;
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
    private CancellationTokenSource? _mediaPreviewCts;
    private string _previewingMediaId = "";
    private MediaLibraryKind? _previewingMediaKind;
    private ObsMediaHideRequest? _mediaPreviewHideRequest;
    private ObsSceneRestoreRequest? _currentObsRestore;
    private ObsMediaHideRequest? _currentObsMediaHide;
    private bool _currentObsCleanedByStop;
    private EventRule? _editingRule;
    private EventRule? _loadedRuleSnapshot;
    private TwitchStreamStatus? _streamStatus;
    private DrawingIcon? _trayIcon;
    private Forms.NotifyIcon? _notifyIcon;

    public ObservableCollection<AudioGroupChoice> AudioGroupChoices { get; } = [];

    public ObservableCollection<AudioAlertChoice> AudioAlertChoices { get; } = [];

    public ObservableCollection<MediaGroupChoice> ImageGroupChoices { get; } = [];

    public ObservableCollection<MediaGroupChoice> VideoGroupChoices { get; } = [];

    public MainWindow()
        : this(AppStartupOptions.Default)
    {
    }

    public MainWindow(AppStartupOptions startupOptions)
    {
        _startupOptions = startupOptions;
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
            for (var i = 0; i < ApplicationLimits.RulePreviewLedDots; i++)
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
            _arduinoMonitorTimer.Interval = TimeSpan.FromSeconds(2.5);
            _arduinoMonitorTimer.Tick += ArduinoMonitorTimer_Tick;
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
            RuleObsSceneBox.ItemsSource = _obsSceneChoices;
            RuleObsSceneBox.DisplayMemberPath = nameof(ObsSceneChoice.Label);
            RuleObsSceneBox.SelectedValuePath = nameof(ObsSceneChoice.Name);
            RuleObsMediaKindBox.ItemsSource = _obsMediaKindOptions;
            RuleObsMediaKindBox.DisplayMemberPath = nameof(UiOption<ObsMediaKind>.Label);
            RuleObsMediaKindBox.SelectedValuePath = nameof(UiOption<ObsMediaKind>.Value);
            RuleObsMediaSourceModeBox.ItemsSource = _mediaSourceModeOptions;
            RuleObsMediaSourceModeBox.DisplayMemberPath = nameof(UiOption<MediaSourceMode>.Label);
            RuleObsMediaSourceModeBox.SelectedValuePath = nameof(UiOption<MediaSourceMode>.Value);
            RuleObsMediaAssetBox.DisplayMemberPath = nameof(MediaAssetConfig.DisplayName);
            RuleObsMediaAssetBox.SelectedValuePath = nameof(MediaAssetConfig.Id);
            RuleObsMediaGroupBox.DisplayMemberPath = nameof(MediaGroupConfig.Name);
            RuleObsMediaGroupBox.SelectedValuePath = nameof(MediaGroupConfig.Id);
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
            ArrangeAlertActionCards();
            RefreshPortList(choosePreferred: false);
        }
        finally
        {
            _loadingUi = false;
        }

        CreateTrayIcon();
        LoadConfigIntoUi();
        _arduinoMonitorTimer.Start();
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

    private void ArrangeAlertActionCards()
    {
        if (ObsActionCard.Parent is not StackPanel parent)
        {
            return;
        }

        var insertIndex = parent.Children.IndexOf(UseLightsActionCard);
        if (insertIndex < 0)
        {
            return;
        }

        var orderedCards = new UIElement[]
        {
            ObsActionCard,
            AudioActionCard,
            ChatActionCard,
            UseLightsActionCard,
            AlexaActionCard
        };

        foreach (var card in orderedCards)
        {
            parent.Children.Remove(card);
        }

        for (var index = 0; index < orderedCards.Length; index++)
        {
            parent.Children.Insert(insertIndex + index, orderedCards[index]);
        }
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


        if (_startupOptions.DebugMode)
        {
            AddLog("Modo debug activo.");
        }

        if (_startupOptions.SuppressAutoConnect)
        {
            AddLog("Conexiones automaticas omitidas por opciones de depuracion.", ActivityLogKind.Important);
        }

        if (_config.StartHidden && !_startupOptions.SuppressStartHidden)
        {
            Hide();
        }

        if (!_startupOptions.SuppressAutoConnect && _config.ArduinoEnabled && _config.AutoConnectArduino && !string.IsNullOrWhiteSpace(_config.SerialPort))
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

        if (!_startupOptions.SuppressAutoConnect && _config.AutoConnectTwitch && _config.Token.HasToken)
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

        if (!_startupOptions.SuppressAutoConnect && _config.Obs.Enabled && _config.Obs.AutoReconnect)
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
