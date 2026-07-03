using System.IO;
using NeoTwitch.Services;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    private void CreateBackup()
    {
        try
        {
            SaveEditableStateFromFields();
            SaveConfig();

            Directory.CreateDirectory(_settingsStore.BackupDirectory);
            var backupPath = Path.Combine(_settingsStore.BackupDirectory, $"settings-manual-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            _settingsStore.Export(_config, backupPath);
            _settingsViewModel.UpdateBackupPathText(_text.Format(UiTextKeys.SettingsManualBackupText, backupPath));
            AddLog(_text.Format(UiTextKeys.SettingsBackupCreatedLog, backupPath));
            _dialog.ShowInformation(_text.Get(UiTextKeys.SettingsBackupTitle), _text.Get(UiTextKeys.SettingsBackupSuccessPrompt));
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, _text.Get(UiTextKeys.SettingsBackupCreateFailureCrash));
            AddLog(_text.Format(UiTextKeys.SettingsBackupCreateFailureLog, ex.Message), ActivityLogKind.Important);
            _dialog.ShowWarning(_text.Get(UiTextKeys.SettingsBackupTitle), ex.Message);
        }
    }

    private async void RestoreBackup()
    {
        var path = _filePicker.OpenFile(new FilePickerRequest(
            _text.Get(UiTextKeys.SettingsRestoreBackupTitle),
            _text.Get(UiTextKeys.SettingsBackupFileFilter),
            InitialDirectory: Directory.Exists(_settingsStore.BackupDirectory)
                ? _settingsStore.BackupDirectory
                : Path.GetDirectoryName(_settingsStore.SettingsPath)));

        if (path is null)
        {
            return;
        }

        if (!ConfirmSettingsReplacement(
                _text.Get(UiTextKeys.SettingsRestoreBackupTitle),
                _text.Get(UiTextKeys.SettingsRestoreBackupPrompt)))
        {
            return;
        }

        try
        {
            await ReplaceSettingsFromFileAsync(
                path,
                _text.Format(UiTextKeys.SettingsBackupRestoredLog, path),
                _text.Get(UiTextKeys.SettingsRestoreBackupTitle),
                _text.Get(UiTextKeys.SettingsBackupRestoreSuccessPrompt));
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, _text.Get(UiTextKeys.SettingsBackupRestoreFailureCrash));
            AddLog(_text.Format(UiTextKeys.SettingsBackupRestoreFailureLog, ex.Message), ActivityLogKind.Important);
            _dialog.ShowWarning(_text.Get(UiTextKeys.SettingsRestoreBackupTitle), ex.Message);
        }
    }
}
