using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Text;

namespace NeoTwitch;

public partial class MainWindow
{
    private readonly AppServices _services = AppServices.CreateDefault();
    private readonly AppStartupOptions _startupOptions;
    private readonly TwitchEventSubClient _eventSubClient;

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
    private IUiTextService _text => _services.Text;
    private Services.Activity.ActivityLogService _activityLog => _services.ActivityLog;
    private Services.Dashboard.DashboardSummaryService _dashboardSummary => _services.DashboardSummary;
    private Services.Alerts.AlertQueueService _alertQueue => _services.AlertQueue;
}
