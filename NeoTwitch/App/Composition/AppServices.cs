using NeoTwitch.Services.Activity;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Dashboard;
using NeoTwitch.Services.Diagnostics;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch.Services;

public sealed class AppServices : IAsyncDisposable
{
    private static readonly TimeSpan AlertShutdownTimeout = TimeSpan.FromSeconds(5);
    private readonly ApplicationResourceOwner _resourceOwner;

    public AppServices(
        SettingsStore settingsStore,
        AudioPlayerService audioPlayer,
        SerialLightController lightController,
        TwitchAuthService authService,
        TwitchChatService chatService,
        AlexaRelayService alexaRelayService,
        ObsWebSocketService obsService,
        ObsOverlayService obsOverlayService,
        VirtualLightsOverlayService virtualLightsOverlayService,
        VirtualLightsScreenOverlayService virtualLightsScreenOverlayService,
        VirtualScreenService virtualScreenService,
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
        AlertExecutionTracker alertExecutionTracker,
        AlertExecutionCoordinator alertExecutionCoordinator,
        ApplicationResourceOwner resourceOwner,
        IDialogService dialog,
        IFilePickerService filePicker,
        IExternalLauncherService externalLauncher,
        IClipboardService clipboard)
    {
        _resourceOwner = resourceOwner;
        SettingsStore = settingsStore;
        AudioPlayer = audioPlayer;
        LightController = lightController;
        AuthService = authService;
        ChatService = chatService;
        AlexaRelayService = alexaRelayService;
        ObsService = obsService;
        ObsOverlayService = obsOverlayService;
        VirtualLightsOverlayService = virtualLightsOverlayService;
        VirtualLightsScreenOverlayService = virtualLightsScreenOverlayService;
        VirtualScreenService = virtualScreenService;
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
        AlertExecutionTracker = alertExecutionTracker;
        AlertExecutionCoordinator = alertExecutionCoordinator;
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

    public VirtualLightsOverlayService VirtualLightsOverlayService { get; }

    public VirtualLightsScreenOverlayService VirtualLightsScreenOverlayService { get; }

    public VirtualScreenService VirtualScreenService { get; }

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

    public AlertExecutionTracker AlertExecutionTracker { get; }

    public AlertExecutionCoordinator AlertExecutionCoordinator { get; }

    public IDialogService Dialog { get; }

    public IFilePickerService FilePicker { get; }

    public IExternalLauncherService ExternalLauncher { get; }

    public IClipboardService Clipboard { get; }

    public IReadOnlyList<ApplicationResourceDisposalFailure> DisposalFailures => _resourceOwner.Failures;

    public void RegisterRuntimeResource(string name, int order, Func<ValueTask> disposeAsync) =>
        _resourceOwner.Register(name, order, disposeAsync);

    public ValueTask DisposeAsync() => _resourceOwner.DisposeAsync();

    public static AppServices CreateDefault()
    {
        var timeProvider = TimeProvider.System;
        var resourceOwner = new ApplicationResourceOwner();
        var activityLog = new ActivityLogService(timeProvider);
        var text = UiTextService.CreateDefault();
        var externalLauncher = new ExternalLauncherService();
        var updateService = new AppUpdateService(text, externalLauncher);
        var alertQueue = new AlertQueueService(timeProvider);
        var alertExecutionTracker = new AlertExecutionTracker(timeProvider);
        var settingsStore = new SettingsStore(text, timeProvider);
        var audioPlayer = new AudioPlayerService(text);
        var lightController = new SerialLightController(text, timeProvider);
        var authService = new TwitchAuthService(text, externalLauncher, timeProvider);
        var chatService = new TwitchChatService(text);
        var alexaRelayService = new AlexaRelayService(text, timeProvider);
        var obsService = new ObsWebSocketService(text);
        var virtualLightsScreenOverlayService = new VirtualLightsScreenOverlayService();
        var alertExecutionCoordinator = new AlertExecutionCoordinator(alertExecutionTracker, alertQueue);

        resourceOwner.Register(
            "Alert execution",
            ApplicationShutdownOrder.ActiveExecution,
            () => StopAndDisposeAlertCoordinatorAsync(alertExecutionCoordinator));
        resourceOwner.Register("Audio players", ApplicationShutdownOrder.VisualMedia, audioPlayer);
        resourceOwner.Register("Virtual lights screen", ApplicationShutdownOrder.VisualMedia, virtualLightsScreenOverlayService);
        resourceOwner.Register("OBS", ApplicationShutdownOrder.Connections, obsService);
        resourceOwner.Register("Arduino", ApplicationShutdownOrder.Connections, lightController);
        resourceOwner.Register("Twitch chat", ApplicationShutdownOrder.NetworkClients, chatService);
        resourceOwner.Register("Twitch authentication", ApplicationShutdownOrder.NetworkClients, authService);
        resourceOwner.Register("Alexa relay", ApplicationShutdownOrder.NetworkClients, alexaRelayService);
        resourceOwner.Register("Updates", ApplicationShutdownOrder.NetworkClients, updateService);
        resourceOwner.Register("Settings", ApplicationShutdownOrder.Persistence, settingsStore);

        return new AppServices(
            settingsStore,
            audioPlayer,
            lightController,
            authService,
            chatService,
            alexaRelayService,
            obsService,
            new ObsOverlayService(timeProvider),
            new VirtualLightsOverlayService(timeProvider),
            virtualLightsScreenOverlayService,
            new VirtualScreenService(),
            new WindowsStartupService(text),
            updateService,
            new DiagnosticReportService(updateService.CheckLatestAsync, text, timeProvider),
            text,
            timeProvider,
            activityLog,
            new ActivityViewModel(activityLog),
            new DashboardSummaryService(),
            new RuleSimulationService(text),
            alertQueue,
            alertExecutionTracker,
            alertExecutionCoordinator,
            resourceOwner,
            new DialogService(),
            new FilePickerService(),
            externalLauncher,
            new ClipboardService());
    }

    private static async ValueTask StopAndDisposeAlertCoordinatorAsync(AlertExecutionCoordinator coordinator)
    {
        if (!await coordinator.StopAsync(AlertShutdownTimeout))
        {
            throw new TimeoutException("Alert execution did not stop within the shutdown timeout.");
        }

        coordinator.Dispose();
    }
}
