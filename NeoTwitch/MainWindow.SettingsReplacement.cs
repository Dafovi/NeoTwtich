using System.Windows;
using NeoTwitch.ViewModels.Activity;
using WpfMessageBox = System.Windows.MessageBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private bool ConfirmSettingsReplacement(string title, string message)
    {
        return WpfMessageBox.Show(
            this,
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes;
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
        WpfMessageBox.Show(
            this,
            successMessage,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
