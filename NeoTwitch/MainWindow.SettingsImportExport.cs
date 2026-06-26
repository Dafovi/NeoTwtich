using System.Windows;
using NeoTwitch.Services;
using NeoTwitch.ViewModels.Activity;
using WpfMessageBox = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace NeoTwitch;

public partial class MainWindow
{
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

        if (!ConfirmSettingsReplacement(
                "Importar configuracion",
                "Importar esta configuracion reemplazara la configuracion actual. Se creara un backup automatico antes de guardar.\n\nQuieres continuar?"))
        {
            return;
        }

        try
        {
            await ReplaceSettingsFromFileAsync(
                dialog.FileName,
                $"Configuracion importada: {dialog.FileName}",
                "Importar configuracion",
                "Configuracion importada correctamente. Revisa Twitch, Arduino y Alexa antes de salir en vivo.");
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo importar la configuracion.");
            AddLog($"Configuracion: no pude importar ({ex.Message}).", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "Importar configuracion", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
