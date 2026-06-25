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
using NeoTwitch.ViewModels.Ui;
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
    private readonly IReadOnlyList<UiOption<TwitchEventKind>> _eventOptions = UiOptionCatalog.EventOptions;
    private readonly IReadOnlyList<UiOption<string>> _ruleCategoryOptions = UiOptionCatalog.RuleCategoryOptions;
    private readonly IReadOnlyList<UiOption<LightPattern>> _patternOptions = UiOptionCatalog.PatternOptions;
    private readonly IReadOnlyList<UiOption<string>> _themeModeOptions = UiOptionCatalog.ThemeModeOptions;
    private readonly IReadOnlyList<UiOption<ObsMediaKind>> _obsMediaKindOptions = UiOptionCatalog.ObsMediaKindOptions;
    private readonly IReadOnlyList<UiOption<MediaSourceMode>> _mediaSourceModeOptions = UiOptionCatalog.MediaSourceModeOptions;

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

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int attributeValue,
        int attributeSize);
}
