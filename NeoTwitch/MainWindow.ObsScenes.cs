using System.Windows;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Obs;
using WpfMessageBox = System.Windows.MessageBox;

namespace NeoTwitch;

public partial class MainWindow
{
    internal async void ObsSceneChangeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ObsSceneRow row })
        {
            return;
        }

        try
        {
            if (!_obsService.IsConnected)
            {
                await ConnectObsAsync();
            }

            var result = await _obsService.SetCurrentProgramSceneAsync(row.Name, CancellationToken.None);
            ApplyObsResult(result);
            AddLog($"OBS: escena cambiada a {row.Name}.", ActivityLogKind.Obs);
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "OBS", MessageBoxButton.OK, MessageBoxImage.Warning);
            UpdateObsStatusText();
        }
    }

    internal async void ObsScenePreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ObsSceneRow row })
        {
            return;
        }

        if (_isObsSceneActionRunning)
        {
            return;
        }

        _isObsSceneActionRunning = true;
        UpdateObsStatusText();

        var previousScene = _obsService.CurrentScene;
        try
        {
            if (!_obsService.IsConnected)
            {
                await ConnectObsAsync();
            }

            if (!_obsService.IsConnected)
            {
                return;
            }

            previousScene = _obsService.CurrentScene;
            var result = await _obsService.SetCurrentProgramSceneAsync(row.Name, CancellationToken.None);
            ApplyObsResult(result);
            AddLog($"OBS: probando escena '{row.Name}' por 5 segundos.", ActivityLogKind.Obs);

            await Task.Delay(TimeSpan.FromSeconds(5));

            if (!string.IsNullOrWhiteSpace(previousScene)
                && !string.Equals(previousScene, row.Name, StringComparison.OrdinalIgnoreCase)
                && _obsService.IsConnected)
            {
                result = await _obsService.SetCurrentProgramSceneAsync(previousScene, CancellationToken.None);
                ApplyObsResult(result);
                AddLog($"OBS: prueba finalizada, regreso a '{previousScene}'.", ActivityLogKind.Obs);
            }
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "OBS", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _isObsSceneActionRunning = false;
            UpdateObsStatusText();
        }
    }
}
