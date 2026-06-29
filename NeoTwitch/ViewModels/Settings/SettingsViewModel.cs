using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Settings;

public sealed class SettingsViewModel : ObservableObject
{
    private Action _importSettings = Noop;
    private Action _exportSettings = Noop;
    private Action _createBackup = Noop;
    private Action _restoreBackup = Noop;
    private Action _runDiagnostics = Noop;
    private Action _save = Noop;

    public SettingsViewModel()
    {
        ImportSettingsCommand = new RelayCommand(() => _importSettings());
        ExportSettingsCommand = new RelayCommand(() => _exportSettings());
        CreateBackupCommand = new RelayCommand(() => _createBackup());
        RestoreBackupCommand = new RelayCommand(() => _restoreBackup());
        RunDiagnosticsCommand = new RelayCommand(() => _runDiagnostics());
        SaveCommand = new RelayCommand(() => _save());
    }

    public RelayCommand ImportSettingsCommand { get; }

    public RelayCommand ExportSettingsCommand { get; }

    public RelayCommand CreateBackupCommand { get; }

    public RelayCommand RestoreBackupCommand { get; }

    public RelayCommand RunDiagnosticsCommand { get; }

    public RelayCommand SaveCommand { get; }

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

    private static void Noop()
    {
    }
}
