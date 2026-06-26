using NeoTwitch.Services.Activity;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Dashboard;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch.Services;

public sealed class AppServices
{
    public AppServices(
        SettingsStore settingsStore,
        AudioPlayerService audioPlayer,
        SerialLightController lightController,
        TwitchAuthService authService,
        TwitchChatService chatService,
        AlexaRelayService alexaRelayService,
        ObsWebSocketService obsService,
        ObsOverlayService obsOverlayService,
        WindowsStartupService windowsStartupService,
        AppUpdateService updateService,
        IUiTextService text,
        ActivityLogService activityLog,
        ActivityViewModel activityViewModel,
        DashboardSummaryService dashboardSummary,
        AlertQueueService alertQueue)
    {
        SettingsStore = settingsStore;
        AudioPlayer = audioPlayer;
        LightController = lightController;
        AuthService = authService;
        ChatService = chatService;
        AlexaRelayService = alexaRelayService;
        ObsService = obsService;
        ObsOverlayService = obsOverlayService;
        WindowsStartupService = windowsStartupService;
        UpdateService = updateService;
        Text = text;
        ActivityLog = activityLog;
        ActivityViewModel = activityViewModel;
        DashboardSummary = dashboardSummary;
        AlertQueue = alertQueue;
    }

    public SettingsStore SettingsStore { get; }

    public AudioPlayerService AudioPlayer { get; }

    public SerialLightController LightController { get; }

    public TwitchAuthService AuthService { get; }

    public TwitchChatService ChatService { get; }

    public AlexaRelayService AlexaRelayService { get; }

    public ObsWebSocketService ObsService { get; }

    public ObsOverlayService ObsOverlayService { get; }

    public WindowsStartupService WindowsStartupService { get; }

    public AppUpdateService UpdateService { get; }

    public IUiTextService Text { get; }

    public ActivityLogService ActivityLog { get; }

    public ActivityViewModel ActivityViewModel { get; }

    public DashboardSummaryService DashboardSummary { get; }

    public AlertQueueService AlertQueue { get; }

    public static AppServices CreateDefault()
    {
        var activityLog = new ActivityLogService();
        return new AppServices(
            new SettingsStore(),
            new AudioPlayerService(),
            new SerialLightController(),
            new TwitchAuthService(),
            new TwitchChatService(),
            new AlexaRelayService(),
            new ObsWebSocketService(),
            new ObsOverlayService(),
            new WindowsStartupService(),
            new AppUpdateService(),
            UiTextService.CreateDefault(),
            activityLog,
            new ActivityViewModel(activityLog),
            new DashboardSummaryService(),
            new AlertQueueService());
    }
}
