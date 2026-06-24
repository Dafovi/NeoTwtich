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
using NeoTwitch.Services.Activity;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Dashboard;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.Shared;
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
    private readonly SettingsStore _settingsStore = new();
    private readonly AudioPlayerService _audioPlayer = new();
    private readonly SerialLightController _lightController = new();
    private readonly TwitchAuthService _authService = new();
    private readonly TwitchChatService _chatService = new();
    private readonly AlexaRelayService _alexaRelayService = new();
    private readonly ObsWebSocketService _obsService = new();
    private readonly ObsOverlayService _obsOverlayService = new();
    private readonly WindowsStartupService _windowsStartupService = new();
    private readonly AppUpdateService _updateService = new();
    private readonly IUiTextService _text = UiTextService.CreateDefault();
    private readonly AppStartupOptions _startupOptions;
    private readonly TwitchEventSubClient _eventSubClient;
    private readonly ActivityLogService _activityLog = new();
    private readonly DashboardSummaryService _dashboardSummary = new();
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
    private string _ruleSearchText = "";
    private string _ruleStatusFilter = EventRuleFilterService.AllStatus;
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
        _config.ThemeMode = ThemeModeService.Normalize(_config.ThemeMode);
        _config.DarkMode = ThemeModeService.ResolveDarkMode(_config.ThemeMode);

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
            _activityViewSource.Source = _activityLog.Entries;
            _activityViewSource.Filter += ActivityViewSource_Filter;
            ActivityList.ItemsSource = _activityViewSource.View;
            DashboardActivityList.ItemsSource = _activityLog.DashboardEntries;
            AudioLibraryList.ItemsSource = _audioLibraryRows;
            AudioGroupsList.ItemsSource = _audioGroupRows;
            ImageLibraryList.ItemsSource = _imageLibraryRows;
            ImageGroupsList.ItemsSource = _imageGroupRows;
            VideoLibraryList.ItemsSource = _videoLibraryRows;
            VideoGroupsList.ItemsSource = _videoGroupRows;
            ObsScenesList.ItemsSource = _obsSceneRows;
            for (var i = 0; i < ApplicationLimits.RulePreviewLedDots; i++)
            {
                _ruleLedPreviewDots.Add(PreviewDot(Services.Lights.LedPreviewService.ParseColor("#334155", "#334155"), 0.08));
                _backgroundLedPreviewDots.Add(PreviewDot(Services.Lights.LedPreviewService.ParseColor("#334155", "#334155"), 0.08));
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
            VersionText.Text = $"V{NeoTwitchProduct.CurrentVersionText}";
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
        foreach (var button in FindVisualChildren<System.Windows.Controls.Button>(this))
        {
            if (IsColorButton(button) || button.Content is not string label)
            {
                continue;
            }

            if (ButtonIconCatalog.TryGetIconKey(label, out var iconKey))
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

        panel.Children.Add(CreateIconPath(IconPathCatalog.Get(iconKey), 15, 1.9));
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
