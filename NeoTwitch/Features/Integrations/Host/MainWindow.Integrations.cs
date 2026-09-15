using NeoTwitch.Services.Integrations;

namespace NeoTwitch;

public partial class MainWindow
{
    private const string CatCamCameraSource = "NeoTwitch · MeowMeowCatCam · Camera";
    private const string CatCamMemeSource = "NeoTwitch · MeowMeowCatCam · Meme";

    internal void OpenCatCamProject() => _externalLauncher.Open(CatCamInstallation.Repository);
    internal string CatCamScene => _config.CatCamScene;
    internal int CatCamOutput => Math.Clamp(_config.CatCamOutput, 0, 2);

    internal async Task<string[]> GetIntegrationScenesAsync(CancellationToken token)
    {
        if (!_obsService.IsConnected) return [];
        await _obsService.RefreshScenesAsync(token);
        return _obsService.Scenes.Select(scene => scene.Name).ToArray();
    }

    internal async Task ShowCatCamAsync(string scene, IReadOnlyDictionary<string, string> windows, int output, CancellationToken token)
    {
        if (!_obsService.IsConnected) throw new InvalidOperationException("Conecta OBS en Conexiones antes de activar la integración.");
        await HideCatCamAsync(scene);
        var canvas = await _obsService.GetCanvasSizeAsync(token);
        var width = output == 0 ? canvas.Width / 2.0 : canvas.Width;
        if (output != 1)
            await _obsService.ShowIntegrationWindowAsync(scene, CatCamCameraSource, windows["Camera"], 0, width, canvas.Height, token);
        if (output != 2)
            await _obsService.ShowIntegrationWindowAsync(scene, CatCamMemeSource, windows["Meme"], output == 0 ? width : 0, width, canvas.Height, token);
        _config.CatCamScene = scene;
        _config.CatCamOutput = output;
        SaveConfig();
    }

    internal async Task HideCatCamAsync(string scene)
    {
        if (!_obsService.IsConnected) return;
        await _obsService.HideSceneSourceAsync(scene, CatCamCameraSource, CancellationToken.None);
        await _obsService.HideSceneSourceAsync(scene, CatCamMemeSource, CancellationToken.None);
    }
}
