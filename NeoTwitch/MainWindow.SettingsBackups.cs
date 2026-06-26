using System.IO;
using System.Windows;
using NeoTwitch.Services;
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
            BackupPathText.Text = $"Ultimo backup manual: {backupPath}";
            AddLog($"Backup creado: {backupPath}");
            WpfMessageBox.Show(this, "Backup creado correctamente.", "Backups", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo crear un backup manual.");
            AddLog($"Backups: no pude crear backup ({ex.Message}).", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "Backups", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    internal async void RestoreBackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = "Restaurar backup",
            Filter = "Backup Neo Twitch (*.json)|*.json|Todos los archivos (*.*)|*.*",
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
                "Restaurar backup",
                "Restaurar este backup reemplazara la configuracion actual. Se creara un backup automatico antes de guardar.\n\nQuieres continuar?"))
        {
            return;
        }

        try
        {
            await ReplaceSettingsFromFileAsync(
                dialog.FileName,
                $"Backup restaurado: {dialog.FileName}",
                "Restaurar backup",
                "Backup restaurado correctamente. Revisa Twitch, Arduino y Alexa antes de salir en vivo.");
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo restaurar el backup.");
            AddLog($"Backups: no pude restaurar ({ex.Message}).", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "Restaurar backup", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
