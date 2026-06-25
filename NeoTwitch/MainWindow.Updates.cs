using System.Windows;
using NeoTwitch.Services;
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
            VersionText.Text = $"V{result.CurrentVersion}";

            if (!result.IsUpdateAvailable)
            {
                AddLog($"Version: V{result.CurrentVersion} al dia.");
                return;
            }

            AddLog($"Version: hay una nueva version V{result.LatestVersion}.", ActivityLogKind.Important);
            var installerPath = _updateService.FindLocalInstallerPath();
            var canUpdateInPlace = !string.IsNullOrWhiteSpace(installerPath);
            var prompt = canUpdateInPlace
                ? $"Hay una nueva version de Neo Twitch.\n\nTu version: V{result.CurrentVersion}\nUltima version: V{result.LatestVersion}\n\nQuieres actualizar ahora? La app se cerrara un momento y el instalador hara el reemplazo."
                : $"Hay una nueva version de Neo Twitch.\n\nTu version: V{result.CurrentVersion}\nUltima version: V{result.LatestVersion}\n\nNo encontre el instalador local. Quieres abrir la pagina de releases para descargarla?";
            var answer = WpfMessageBox.Show(
                this,
                prompt,
                "Actualizacion disponible",
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
            AddLog($"Version: no pude consultar actualizaciones ({ex.Message}).");
        }
    }

    private async Task LaunchInstallerUpdateAsync(string installerPath, VersionCheckResult result)
    {
        try
        {
            _updateService.LaunchInstallerUpdate(installerPath, result);
            AddLog($"Version: iniciando actualizador a V{result.LatestVersion}.", ActivityLogKind.Important);
            await ExitApplicationAsync();
        }
        catch (Exception ex)
        {
            AddLog($"Version: no pude abrir el actualizador ({ex.Message}).", ActivityLogKind.Important);
            _updateService.OpenReleasePage(result.ReleaseUrl);
        }
    }
}
