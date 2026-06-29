using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.ViewModels.Shell;

namespace NeoTwitch;

public partial class MainWindow : Window
{
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

        _shellViewModel = new ShellViewModel(_text, TryNavigateToTab);
        DataContext = _shellViewModel;

        _eventSubClient = new TwitchEventSubClient(_authService, () => _config, SaveConfig, AddLog, _text);
        _eventSubClient.EventReceived += EventSubClient_EventReceived;

        InitializeRuntimeUi();
        CreateTrayIcon();
        LoadConfigIntoUi();
        _arduinoMonitorTimer.Start();
    }

}
