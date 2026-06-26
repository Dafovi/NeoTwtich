using System.IO;
using System.Windows;
using NeoTwitch.Services;
using NeoTwitch.ViewModels.Activity;
using WpfMessageBox = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace NeoTwitch;

public partial class MainWindow
{
    internal async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveEditableStateFromFields();
        SaveConfig();
        await ApplyBackgroundStateAsync();
        AddLog("Configuracion guardada.");
    }

    internal void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveEditableStateFromFields();
            SaveConfig();

            var dialog = new WpfSaveFileDialog
            {
                Title = "Exportar configuracion",
                FileName = $"NeoTwitch-config-{DateTime.Now:yyyyMMdd-HHmmss}.json",
                Filter = "Configuracion Neo Twitch (*.json)|*.json|Todos los archivos (*.*)|*.*",
                AddExtension = true,
                DefaultExt = ".json",
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            _settingsStore.Export(_config, dialog.FileName);
            AddLog($"Configuracion exportada: {dialog.FileName}");
            WpfMessageBox.Show(
                this,
                "Configuracion exportada correctamente.\n\nEste archivo puede incluir tokens, URLs o secretos privados. Guardalo en un lugar seguro.",
                "Configuracion",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo exportar la configuracion.");
            AddLog($"Configuracion: no pude exportar ({ex.Message}).", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "Exportar configuracion", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

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

    internal async void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = "Importar configuracion",
            Filter = "Configuracion Neo Twitch (*.json)|*.json|Todos los archivos (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var confirm = WpfMessageBox.Show(
            this,
            "Importar esta configuracion reemplazara la configuracion actual. Se creara un backup automatico antes de guardar.\n\nQuieres continuar?",
            "Importar configuracion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            if (_eventSubClient.IsRunning)
            {
                await _eventSubClient.StopAsync();
                _eventSubscriptionSignature = "";
                _streamStatus = null;
            }

            _config = _settingsStore.Import(dialog.FileName);
            LoadConfigIntoUi();
            AddLog($"Configuracion importada: {dialog.FileName}", ActivityLogKind.Important);
            WpfMessageBox.Show(
                this,
                "Configuracion importada correctamente. Revisa Twitch, Arduino y Alexa antes de salir en vivo.",
                "Importar configuracion",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo importar la configuracion.");
            AddLog($"Configuracion: no pude importar ({ex.Message}).", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "Importar configuracion", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        var confirm = WpfMessageBox.Show(
            this,
            "Restaurar este backup reemplazara la configuracion actual. Se creara un backup automatico antes de guardar.\n\nQuieres continuar?",
            "Restaurar backup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            if (_eventSubClient.IsRunning)
            {
                await _eventSubClient.StopAsync();
                _eventSubscriptionSignature = "";
                _streamStatus = null;
            }

            _config = _settingsStore.Import(dialog.FileName);
            LoadConfigIntoUi();
            AddLog($"Backup restaurado: {dialog.FileName}", ActivityLogKind.Important);
            WpfMessageBox.Show(
                this,
                "Backup restaurado correctamente. Revisa Twitch, Arduino y Alexa antes de salir en vivo.",
                "Restaurar backup",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo restaurar el backup.");
            AddLog($"Backups: no pude restaurar ({ex.Message}).", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "Restaurar backup", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
