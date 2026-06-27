using System.Windows;
using NeoTwitch.Services;
using NeoTwitch.Services.Text;
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
                Title = _text.Get(UiTextKeys.SettingsExportTitle),
                FileName = $"NeoTwitch-config-{DateTime.Now:yyyyMMdd-HHmmss}.json",
                Filter = _text.Get(UiTextKeys.SettingsConfigFileFilter),
                AddExtension = true,
                DefaultExt = ".json",
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            _settingsStore.Export(_config, dialog.FileName);
            AddLog(_text.Format(UiTextKeys.SettingsExportedLog, dialog.FileName));
            WpfMessageBox.Show(
                this,
                _text.Get(UiTextKeys.SettingsExportSuccessPrompt),
                _text.Get(UiTextKeys.SettingsTitle),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, _text.Get(UiTextKeys.SettingsExportFailureCrash));
            AddLog(_text.Format(UiTextKeys.SettingsExportFailureLog, ex.Message), ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, _text.Get(UiTextKeys.SettingsExportTitle), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    internal async void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = _text.Get(UiTextKeys.SettingsImportTitle),
            Filter = _text.Get(UiTextKeys.SettingsConfigFileFilter),
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        if (!ConfirmSettingsReplacement(
                _text.Get(UiTextKeys.SettingsImportTitle),
                _text.Get(UiTextKeys.SettingsImportPrompt)))
        {
            return;
        }

        try
        {
            await ReplaceSettingsFromFileAsync(
                dialog.FileName,
                _text.Format(UiTextKeys.SettingsImportedLog, dialog.FileName),
                _text.Get(UiTextKeys.SettingsImportTitle),
                _text.Get(UiTextKeys.SettingsImportSuccessPrompt));
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, _text.Get(UiTextKeys.SettingsImportFailureCrash));
            AddLog(_text.Format(UiTextKeys.SettingsImportFailureLog, ex.Message), ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, _text.Get(UiTextKeys.SettingsImportTitle), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
