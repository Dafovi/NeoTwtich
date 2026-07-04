using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Diagnostics;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Alexa;
using NeoTwitch.ViewModels.Alerts;
using NeoTwitch.ViewModels.Connections;
using NeoTwitch.ViewModels.Dashboard;
using NeoTwitch.ViewModels.Lights;
using NeoTwitch.ViewModels.Obs;
using NeoTwitch.ViewModels.Settings;
using NeoTwitch.ViewModels.Shell;

namespace NeoTwitch;

public partial class MainWindow
{
    private readonly AppServices _services = AppServices.CreateDefault();
    private readonly AppStartupOptions _startupOptions;
    private readonly TwitchEventSubClient _eventSubClient;
    private ShellViewModel _shellViewModel = null!;
    private AlertsViewModel _alertsViewModel = null!;
    private AlexaViewModel _alexaViewModel = null!;
    private DashboardViewModel _dashboardViewModel = null!;
    private ConnectionsViewModel _connectionsViewModel = null!;
    private LightsViewModel _lightsViewModel = null!;
    private ObsViewModel _obsViewModel = null!;
    private SettingsViewModel _settingsViewModel = null!;

    private SettingsStore _settingsStore => _services.SettingsStore;
    private AudioPlayerService _audioPlayer => _services.AudioPlayer;
    private SerialLightController _lightController => _services.LightController;
    private TwitchAuthService _authService => _services.AuthService;
    private TwitchChatService _chatService => _services.ChatService;
    private AlexaRelayService _alexaRelayService => _services.AlexaRelayService;
    private ObsWebSocketService _obsService => _services.ObsService;
    private ObsOverlayService _obsOverlayService => _services.ObsOverlayService;
    private WindowsStartupService _windowsStartupService => _services.WindowsStartupService;
    private AppUpdateService _updateService => _services.UpdateService;
    private DiagnosticReportService _diagnosticReportService => _services.DiagnosticReportService;
    private TimeProvider _timeProvider => _services.TimeProvider;
    private IUiTextService _text => _services.Text;
    private Services.Activity.ActivityLogService _activityLog => _services.ActivityLog;
    private ActivityViewModel _activityViewModel => _services.ActivityViewModel;
    private Services.Dashboard.DashboardSummaryService _dashboardSummary => _services.DashboardSummary;
    private Services.Alerts.RuleSimulationService _ruleSimulation => _services.RuleSimulation;
    private Services.Alerts.AlertQueueService _alertQueue => _services.AlertQueue;
    private Services.Ui.IDialogService _dialog => _services.Dialog;
    private Services.Ui.IFilePickerService _filePicker => _services.FilePicker;
    private Services.Ui.IExternalLauncherService _externalLauncher => _services.ExternalLauncher;
    private Services.Ui.IClipboardService _clipboard => _services.Clipboard;
}
