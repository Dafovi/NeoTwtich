using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    private bool ConfirmSettingsReplacement(string title, string message)
    {
        return _dialog.Confirm(title, message);
    }

    private async Task ReplaceSettingsFromFileAsync(
        string path,
        string logMessage,
        string title,
        string successMessage)
    {
        if (_eventSubClient.IsRunning)
        {
            await _eventSubClient.StopAsync();
            _eventSubscriptionSignature = "";
            _streamStatus = null;
        }

        _config = _settingsStore.Import(path);
        LoadConfigIntoUi();
        AddLog(logMessage, ActivityLogKind.Important);
        _dialog.ShowInformation(title, successMessage);
    }
}
