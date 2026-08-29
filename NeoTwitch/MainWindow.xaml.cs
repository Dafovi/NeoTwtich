using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Shell;

namespace NeoTwitch;

public partial class MainWindow : Window, IAlertExecutionCapabilities
{
    public MainWindow()
        : this(AppStartupOptions.Default)
    {
    }

    public MainWindow(AppStartupOptions startupOptions)
    {
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
        _eventSubClient.EventReceivedAsync += EventSubClient_EventReceivedAsync;
        _eventSubClient.HealthChanged += EventSubClient_HealthChanged;

        InitializeRuntimeUi();
        CreateTrayIcon();
        LoadConfigIntoUi();
        _arduinoMonitorTimer.Start();
    }

}
