using System.Collections;
using NeoTwitch.Models;
using NeoTwitch.ViewModels.Core;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;

namespace NeoTwitch.ViewModels.Settings;

public sealed class SettingsViewModel : ObservableObject
{
    private string _settingsPathText = "";
    private string _backupPathText = "";
    private string _versionText = "";
    private string _diagnosticStatusText = "Estado: Todo en orden";
    private string _appStateIconPath = "/Assets/Icons/appstate_ok.png";
    private WpfBrush _diagnosticStatusBrush = WpfBrushes.LimeGreen;
    private IEnumerable? _themeModeChoices;
    private bool _startHidden;
    private bool _startWithWindows;
    private string _themeMode = "System";
    private bool _closeToTray = true;
    private bool _autoConnectTwitch;
    private bool _autoConnectArduino;
    private bool _obsAutoReconnect;
    private string _maxQueuedSameRuleAlertsText = "1";
    private string _sameRuleQueueCooldownText = "0";
    private string _maxQueuedDifferentRuleAlertsText = "3";
    private string _differentRuleQueueCooldownText = "0";
    private Action _importSettings = Noop;
    private Action _exportSettings = Noop;
    private Action _createBackup = Noop;
    private Action _restoreBackup = Noop;
    private Action _runDiagnostics = Noop;
    private Action _save = Noop;
    private Action<object?> _selectCloseBehavior = Noop;

    public SettingsViewModel()
    {
        ImportSettingsCommand = new RelayCommand(() => _importSettings());
        ExportSettingsCommand = new RelayCommand(() => _exportSettings());
        CreateBackupCommand = new RelayCommand(() => _createBackup());
        RestoreBackupCommand = new RelayCommand(() => _restoreBackup());
        RunDiagnosticsCommand = new RelayCommand(() => _runDiagnostics());
        SaveCommand = new RelayCommand(() => _save());
        SelectCloseBehaviorCommand = new RelayCommand(parameter => _selectCloseBehavior(parameter));
    }

    public RelayCommand ImportSettingsCommand { get; }

    public RelayCommand ExportSettingsCommand { get; }

    public RelayCommand CreateBackupCommand { get; }

    public RelayCommand RestoreBackupCommand { get; }

    public RelayCommand RunDiagnosticsCommand { get; }

    public RelayCommand SaveCommand { get; }

    public RelayCommand SelectCloseBehaviorCommand { get; }

    public string SettingsPathText
    {
        get => _settingsPathText;
        private set => SetProperty(ref _settingsPathText, value);
    }

    public string BackupPathText
    {
        get => _backupPathText;
        private set => SetProperty(ref _backupPathText, value);
    }

    public string VersionText
    {
        get => _versionText;
        private set => SetProperty(ref _versionText, value);
    }

    public string DiagnosticStatusText
    {
        get => _diagnosticStatusText;
        private set => SetProperty(ref _diagnosticStatusText, value);
    }

    public WpfBrush DiagnosticStatusBrush
    {
        get => _diagnosticStatusBrush;
        private set => SetProperty(ref _diagnosticStatusBrush, value);
    }

    public string AppStateIconPath
    {
        get => _appStateIconPath;
        private set => SetProperty(ref _appStateIconPath, value);
    }

    public IEnumerable? ThemeModeChoices
    {
        get => _themeModeChoices;
        private set => SetProperty(ref _themeModeChoices, value);
    }

    public bool StartHidden
    {
        get => _startHidden;
        set => SetProperty(ref _startHidden, value);
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => SetProperty(ref _startWithWindows, value);
    }

    public string ThemeMode
    {
        get => _themeMode;
        set => SetProperty(ref _themeMode, value ?? "System");
    }

    public bool CloseToTray
    {
        get => _closeToTray;
        set => SetProperty(ref _closeToTray, value);
    }

    public bool AutoConnectTwitch
    {
        get => _autoConnectTwitch;
        set => SetProperty(ref _autoConnectTwitch, value);
    }

    public bool AutoConnectArduino
    {
        get => _autoConnectArduino;
        set => SetProperty(ref _autoConnectArduino, value);
    }

    public bool ObsAutoReconnect
    {
        get => _obsAutoReconnect;
        set => SetProperty(ref _obsAutoReconnect, value);
    }

    public string MaxQueuedSameRuleAlertsText
    {
        get => _maxQueuedSameRuleAlertsText;
        set => SetProperty(ref _maxQueuedSameRuleAlertsText, value ?? "");
    }

    public string SameRuleQueueCooldownText
    {
        get => _sameRuleQueueCooldownText;
        set => SetProperty(ref _sameRuleQueueCooldownText, value ?? "");
    }

    public string MaxQueuedDifferentRuleAlertsText
    {
        get => _maxQueuedDifferentRuleAlertsText;
        set => SetProperty(ref _maxQueuedDifferentRuleAlertsText, value ?? "");
    }

    public string DifferentRuleQueueCooldownText
    {
        get => _differentRuleQueueCooldownText;
        set => SetProperty(ref _differentRuleQueueCooldownText, value ?? "");
    }

    public void LoadPreferences(AppConfig config)
    {
        StartHidden = config.StartHidden;
        StartWithWindows = config.StartWithWindows;
        ThemeMode = config.ThemeMode;
        CloseToTray = config.CloseToTray;
        AutoConnectTwitch = config.AutoConnectTwitch;
        AutoConnectArduino = config.AutoConnectArduino;
        ObsAutoReconnect = config.Obs.AutoReconnect;
        MaxQueuedSameRuleAlertsText = config.MaxQueuedSameRuleAlerts.ToString();
        SameRuleQueueCooldownText = config.SameRuleQueueCooldownMs.ToString();
        MaxQueuedDifferentRuleAlertsText = config.MaxQueuedDifferentRuleAlerts.ToString();
        DifferentRuleQueueCooldownText = config.DifferentRuleQueueCooldownMs.ToString();
    }

    public void ConfigureActions(
        Action importSettings,
        Action exportSettings,
        Action createBackup,
        Action restoreBackup,
        Action runDiagnostics,
        Action save)
    {
        _importSettings = importSettings;
        _exportSettings = exportSettings;
        _createBackup = createBackup;
        _restoreBackup = restoreBackup;
        _runDiagnostics = runDiagnostics;
        _save = save;
    }

    public void ConfigureEditorActions(Action<object?> selectCloseBehavior)
    {
        _selectCloseBehavior = selectCloseBehavior;
    }

    public void UpdateMetadata(string settingsPath, string backupPath, string versionText)
    {
        SettingsPathText = settingsPath;
        BackupPathText = backupPath;
        VersionText = versionText;
    }

    public void UpdateBackupPathText(string backupPath)
    {
        BackupPathText = backupPath;
    }

    public void UpdateAppState(string statusText, WpfBrush statusBrush, string iconPath)
    {
        DiagnosticStatusText = statusText;
        DiagnosticStatusBrush = statusBrush;
        AppStateIconPath = iconPath;
    }

    public void UpdateThemeModeChoices(IEnumerable? choices)
    {
        ThemeModeChoices = choices;
    }

    private static void Noop()
    {
    }

    private static void Noop(object? _)
    {
    }
}
