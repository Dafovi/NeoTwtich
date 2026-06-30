using System.Windows;
using NeoTwitch.Services;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;
using WpfMessageBox = System.Windows.MessageBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var result = await _updateService.CheckLatestAsync(CancellationToken.None);

            if (!result.IsUpdateAvailable)
            {
                AddLog(_text.Format(UiTextKeys.UpdateUpToDateLog, result.CurrentVersion));
                return;
            }

            AddLog(_text.Format(UiTextKeys.UpdateAvailableLog, result.LatestVersion), ActivityLogKind.Important);
            var installerPath = _updateService.FindLocalInstallerPath();
            var canUpdateInPlace = !string.IsNullOrWhiteSpace(installerPath);
            var prompt = canUpdateInPlace
                ? _text.Format(UiTextKeys.UpdatePromptInPlace, result.CurrentVersion, result.LatestVersion)
                : _text.Format(UiTextKeys.UpdatePromptReleasePage, result.CurrentVersion, result.LatestVersion);
            var answer = WpfMessageBox.Show(
                this,
                prompt,
                _text.Get(UiTextKeys.UpdateAvailableTitle),
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (answer == MessageBoxResult.Yes)
            {
                if (canUpdateInPlace)
                {
                    await LaunchInstallerUpdateAsync(installerPath, result);
                }
                else
                {
                    _updateService.OpenReleasePage(result.ReleaseUrl);
                }
            }
        }
        catch (Exception ex)
        {
            AddLog(_text.Format(UiTextKeys.UpdateCheckFailedLog, ex.Message));
        }
    }

    private async Task LaunchInstallerUpdateAsync(string installerPath, VersionCheckResult result)
    {
        try
        {
            _updateService.LaunchInstallerUpdate(installerPath, result);
            AddLog(_text.Format(UiTextKeys.UpdateLaunchingInstallerLog, result.LatestVersion), ActivityLogKind.Important);
            await ExitApplicationAsync();
        }
        catch (Exception ex)
        {
            AddLog(_text.Format(UiTextKeys.UpdateLaunchFailedLog, ex.Message), ActivityLogKind.Important);
            _updateService.OpenReleasePage(result.ReleaseUrl);
        }
    }
}
