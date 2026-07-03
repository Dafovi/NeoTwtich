using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Obs;

namespace NeoTwitch;

public partial class MainWindow
{
    private async void ChangeObsScene(object? parameter)
    {
        var sceneName = ResolveObsSceneName(parameter);
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        try
        {
            if (!_obsService.IsConnected)
            {
                await ConnectObsAsync();
            }

            var result = await _obsService.SetCurrentProgramSceneAsync(sceneName, CancellationToken.None);
            ApplyObsResult(result);
            AddLog($"OBS: escena cambiada a {sceneName}.", ActivityLogKind.Obs);
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            _dialog.ShowWarning("OBS", ex.Message);
            UpdateObsStatusText();
        }
    }

    private async void PreviewObsScene(object? parameter)
    {
        var sceneName = ResolveObsSceneName(parameter);
        if (string.IsNullOrWhiteSpace(sceneName))
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
            var result = await _obsService.SetCurrentProgramSceneAsync(sceneName, CancellationToken.None);
            ApplyObsResult(result);
            AddLog($"OBS: probando escena '{sceneName}' por 5 segundos.", ActivityLogKind.Obs);

            await Task.Delay(TimeSpan.FromSeconds(5));

            if (!string.IsNullOrWhiteSpace(previousScene)
                && !string.Equals(previousScene, sceneName, StringComparison.OrdinalIgnoreCase)
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
            _dialog.ShowWarning("OBS", ex.Message);
        }
        finally
        {
            _isObsSceneActionRunning = false;
            UpdateObsStatusText();
        }
    }

    private static string ResolveObsSceneName(object? parameter)
    {
        return parameter switch
        {
            ObsSceneRow row => row.Name,
            string sceneName => sceneName,
            _ => ""
        };
    }
}
