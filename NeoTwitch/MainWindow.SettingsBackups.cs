using System.IO;
using System.Windows;
using NeoTwitch.Services;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;
using WpfMessageBox = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void CreateBackupButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveEditableStateFromFields();
            SaveConfig();

            Directory.CreateDirectory(_settingsStore.BackupDirectory);
            var backupPath = Path.Combine(_settingsStore.BackupDirectory, $"settings-manual-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            _settingsStore.Export(_config, backupPath);
            BackupPathText.Text = _text.Format(UiTextKeys.SettingsManualBackupText, backupPath);
            AddLog(_text.Format(UiTextKeys.SettingsBackupCreatedLog, backupPath));
            WpfMessageBox.Show(this, _text.Get(UiTextKeys.SettingsBackupSuccessPrompt), _text.Get(UiTextKeys.SettingsBackupTitle), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, _text.Get(UiTextKeys.SettingsBackupCreateFailureCrash));
            AddLog(_text.Format(UiTextKeys.SettingsBackupCreateFailureLog, ex.Message), ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, _text.Get(UiTextKeys.SettingsBackupTitle), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    internal async void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = _text.Get(UiTextKeys.SettingsRestoreBackupTitle),
            Filter = _text.Get(UiTextKeys.SettingsBackupFileFilter),
            CheckFileExists = true,
            InitialDirectory = Directory.Exists(_settingsStore.BackupDirectory)
                ? _settingsStore.BackupDirectory
                : Path.GetDirectoryName(_settingsStore.SettingsPath)
        };

        if (dialog.ShowDialog(this) != true)
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
                dialog.FileName,
                _text.Format(UiTextKeys.SettingsBackupRestoredLog, dialog.FileName),
                _text.Get(UiTextKeys.SettingsRestoreBackupTitle),
                _text.Get(UiTextKeys.SettingsBackupRestoreSuccessPrompt));
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, _text.Get(UiTextKeys.SettingsBackupRestoreFailureCrash));
            AddLog(_text.Format(UiTextKeys.SettingsBackupRestoreFailureLog, ex.Message), ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, _text.Get(UiTextKeys.SettingsRestoreBackupTitle), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
