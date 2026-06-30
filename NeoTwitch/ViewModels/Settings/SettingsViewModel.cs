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

    private static void Noop()
    {
    }

    private static void Noop(object? _)
    {
    }
}
