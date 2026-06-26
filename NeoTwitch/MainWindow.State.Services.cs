using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Activity;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Dashboard;
using NeoTwitch.Services.Text;

namespace NeoTwitch;

public partial class MainWindow
{
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
    private readonly AlertQueueService _alertQueue = new();
}
