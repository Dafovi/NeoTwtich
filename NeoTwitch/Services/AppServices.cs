using NeoTwitch.Services.Activity;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Dashboard;
using NeoTwitch.Services.Diagnostics;
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
        DiagnosticReportService diagnosticReportService,
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
        DiagnosticReportService = diagnosticReportService;
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

    public DiagnosticReportService DiagnosticReportService { get; }

    public IUiTextService Text { get; }

    public ActivityLogService ActivityLog { get; }

    public ActivityViewModel ActivityViewModel { get; }

    public DashboardSummaryService DashboardSummary { get; }

    public AlertQueueService AlertQueue { get; }

    public static AppServices CreateDefault()
    {
        var activityLog = new ActivityLogService();
        var updateService = new AppUpdateService();
        var text = UiTextService.CreateDefault();
        return new AppServices(
            new SettingsStore(text),
            new AudioPlayerService(),
            new SerialLightController(text),
            new TwitchAuthService(),
            new TwitchChatService(),
            new AlexaRelayService(text),
            new ObsWebSocketService(text),
            new ObsOverlayService(),
            new WindowsStartupService(text),
            updateService,
            new DiagnosticReportService(updateService.CheckLatestAsync, text),
            text,
            activityLog,
            new ActivityViewModel(activityLog),
            new DashboardSummaryService(),
            new AlertQueueService());
    }
}
