using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Ui;
using NeoTwitch.Services.Streaming;
using NeoTwitch.ViewModels.Shell;

namespace NeoTwitch;

public partial class MainWindow : Window, IAlertExecutionCapabilities
{
    public MainWindow()
        : this(AppStartupOptions.Default, AppServices.CreateDefault())
    {
    }

    public MainWindow(AppStartupOptions startupOptions)
        : this(startupOptions, AppServices.CreateDefault())
    {
    }

    internal MainWindow(AppStartupOptions startupOptions, AppServices services)
    {
        _services = services;
        _startupOptions = startupOptions;
        _eventOptions = UiOptionCatalog.CreateEventOptions(_text);
        _ruleCategoryOptions = UiOptionCatalog.CreateRuleCategoryOptions(_text);
        _patternOptions = UiOptionCatalog.CreatePatternOptions(_text);
        _themeModeOptions = UiOptionCatalog.CreateThemeModeOptions(_text);
        _obsMediaKindOptions = UiOptionCatalog.CreateObsMediaKindOptions(_text);
        _mediaSourceModeOptions = UiOptionCatalog.CreateMediaSourceModeOptions(_text);
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

        _shellViewModel = new ShellViewModel(_text, TryNavigateToTab);
        DataContext = _shellViewModel;

        _eventSubClient = new TwitchEventSubClient(_authService, () => _config, SaveConfig, AddLog, _text);
        _twitchPlatformProvider = new TwitchStreamingPlatformProvider(_eventSubClient);
        _youTubePlatformProvider = new YouTubeStreamingPlatformProvider(
            () => _config,
            _youTubeAuthService,
            _youTubeLiveService,
            _youTubeLiveChatService,
            SaveConfig,
            AddLog,
            _timeProvider);
        _services.RegisterRuntimeResource(
            "Twitch EventSub",
            ApplicationShutdownOrder.EventIngress,
            DisposeEventSubAsync);
        _services.RegisterRuntimeResource(
            "YouTube live chat",
            ApplicationShutdownOrder.EventIngress,
            DisposeYouTubeLiveChatAsync);
        _twitchPlatformProvider.EventReceivedAsync += PlatformEventRouter_PublishAsync;
        _youTubePlatformProvider.EventReceivedAsync += PlatformEventRouter_PublishAsync;
        _platformEventRouter.EventReceivedAsync += PlatformEventRouter_EventReceivedAsync;
        _eventSubClient.HealthChanged += EventSubClient_HealthChanged;

        InitializeRuntimeUi();
        CreateTrayIcon();
        LoadConfigIntoUi();
        _arduinoMonitorTimer.Start();
    }

    private async ValueTask DisposeEventSubAsync()
    {
        try
        {
            await _twitchPlatformProvider.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
            CrashReporter.LogMessage("EventSub no terminó dentro de los 5 segundos de cierre; se forzó la liberación local.");
        }
        finally
        {
            await _twitchPlatformProvider.DisposeAsync();
            _eventSubClient.Dispose();
        }
    }

    private async ValueTask DisposeYouTubeLiveChatAsync()
    {
        await _youTubePlatformProvider.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
        await _youTubePlatformProvider.DisposeAsync();
    }

    private Task PlatformEventRouter_PublishAsync(StreamEvent streamEvent, CancellationToken cancellationToken) =>
        _platformEventRouter.PublishAsync(streamEvent, cancellationToken);

}
