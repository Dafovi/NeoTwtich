using NeoTwitch.Services.Activity;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Dashboard;
using NeoTwitch.Services.Diagnostics;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
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
        TimeProvider timeProvider,
        ActivityLogService activityLog,
        ActivityViewModel activityViewModel,
        DashboardSummaryService dashboardSummary,
        RuleSimulationService ruleSimulation,
        AlertQueueService alertQueue,
        IDialogService dialog,
        IFilePickerService filePicker,
        IExternalLauncherService externalLauncher,
        IClipboardService clipboard)
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
        TimeProvider = timeProvider;
        ActivityLog = activityLog;
        ActivityViewModel = activityViewModel;
        DashboardSummary = dashboardSummary;
        RuleSimulation = ruleSimulation;
        AlertQueue = alertQueue;
        Dialog = dialog;
        FilePicker = filePicker;
        ExternalLauncher = externalLauncher;
        Clipboard = clipboard;
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

    public TimeProvider TimeProvider { get; }

    public ActivityLogService ActivityLog { get; }

    public ActivityViewModel ActivityViewModel { get; }

    public DashboardSummaryService DashboardSummary { get; }

    public RuleSimulationService RuleSimulation { get; }

    public AlertQueueService AlertQueue { get; }

    public IDialogService Dialog { get; }

    public IFilePickerService FilePicker { get; }

    public IExternalLauncherService ExternalLauncher { get; }

    public IClipboardService Clipboard { get; }

    public static AppServices CreateDefault()
    {
        var timeProvider = TimeProvider.System;
        var activityLog = new ActivityLogService(timeProvider);
        var text = UiTextService.CreateDefault();
        var externalLauncher = new ExternalLauncherService();
        var updateService = new AppUpdateService(text, externalLauncher);
        return new AppServices(
            new SettingsStore(text, timeProvider),
            new AudioPlayerService(text),
            new SerialLightController(text),
            new TwitchAuthService(text, externalLauncher),
            new TwitchChatService(text),
            new AlexaRelayService(text),
            new ObsWebSocketService(text),
            new ObsOverlayService(timeProvider),
            new WindowsStartupService(text),
            updateService,
            new DiagnosticReportService(updateService.CheckLatestAsync, text, timeProvider),
            text,
            timeProvider,
            activityLog,
            new ActivityViewModel(activityLog),
            new DashboardSummaryService(),
            new RuleSimulationService(text),
            new AlertQueueService(timeProvider),
            new DialogService(),
            new FilePickerService(),
            externalLauncher,
            new ClipboardService());
    }
}
