using NeoTwitch.Services;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    private void ExportSettings()
    {
        try
        {
            SaveEditableStateFromFields();
            SaveConfig();

            var path = _filePicker.SaveFile(new FilePickerRequest(
                _text.Get(UiTextKeys.SettingsExportTitle),
                _text.Get(UiTextKeys.SettingsConfigFileFilter),
                FileName: $"NeoTwitch-config-{DateTime.Now:yyyyMMdd-HHmmss}.json",
                DefaultExtension: ".json",
                OverwritePrompt: true));

            if (path is null)
            {
                return;
            }

            _settingsStore.Export(_config, path);
            AddLog(_text.Format(UiTextKeys.SettingsExportedLog, path));
            _dialog.ShowInformation(
                _text.Get(UiTextKeys.SettingsTitle),
                _text.Get(UiTextKeys.SettingsExportSuccessPrompt));
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, _text.Get(UiTextKeys.SettingsExportFailureCrash));
            AddLog(_text.Format(UiTextKeys.SettingsExportFailureLog, ex.Message), ActivityLogKind.Important);
            _dialog.ShowWarning(_text.Get(UiTextKeys.SettingsExportTitle), ex.Message);
        }
    }

    private async void ImportSettings()
    {
        var path = _filePicker.OpenFile(new FilePickerRequest(
            _text.Get(UiTextKeys.SettingsImportTitle),
            _text.Get(UiTextKeys.SettingsConfigFileFilter)));

        if (path is null)
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
                path,
                _text.Format(UiTextKeys.SettingsImportedLog, path),
                _text.Get(UiTextKeys.SettingsImportTitle),
                _text.Get(UiTextKeys.SettingsImportSuccessPrompt));
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, _text.Get(UiTextKeys.SettingsImportFailureCrash));
            AddLog(_text.Format(UiTextKeys.SettingsImportFailureLog, ex.Message), ActivityLogKind.Important);
            _dialog.ShowWarning(_text.Get(UiTextKeys.SettingsImportTitle), ex.Message);
        }
    }
}
