using NeoTwitch.Models;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    private string BuildVirtualLightsOverlayUrl()
    {
        return _virtualLightsOverlayService.BuildOverlayUrl();
    }

    private async Task<TimeSpan?> StartRuleVirtualLightsAsync(EventRule rule, int? synchronizedDurationMs, CancellationToken cancellationToken)
    {
        if (!rule.UseVirtualLights || (!rule.VirtualLightsToObs && !rule.VirtualLightsToScreen))
        {
            return null;
        }

        var command = VirtualLightCommand.FromRule(rule, synchronizedDurationMs);
        var duration = TimeSpan.FromMilliseconds(command.DurationMs);

        if (rule.VirtualLightsToObs)
        {
            try
            {
                _virtualLightsOverlayService.WriteState(command, duration);
                await ShowVirtualLightsBrowserSourceAsync(command, duration, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                AddLog($"Luces virtuales OBS: {ex.Message}", ActivityLogKind.Important);
            }
        }

        if (rule.VirtualLightsToScreen)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var screen = _virtualScreenService.ResolveScreen(rule.VirtualLightsScreenId);
                await _virtualLightsScreenOverlayService.ShowAsync(command, screen);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                AddLog($"Luces virtuales pantalla: {ex.Message}", ActivityLogKind.Important);
            }
        }

        AddLog("Luces virtuales activadas.", ActivityLogKind.Event);
        return duration;
    }

    private async Task ShowVirtualLightsBrowserSourceAsync(
        VirtualLightCommand command,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        if (!_config.Obs.IsConfigured)
        {
            AddLog("Luces virtuales OBS: conecta OBS para mostrar el overlay.", ActivityLogKind.Important);
            return;
        }

        if (!_obsService.IsConnected)
        {
            await ConnectObsAsync(cancellationToken);
        }

        if (!_obsService.IsConnected)
        {
            return;
        }

        var sceneName = _obsService.CurrentScene;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            AddLog("Luces virtuales OBS: no hay escena activa.", ActivityLogKind.Important);
            return;
        }

        var width = _config.Obs.OverlayWidth;
        var height = _config.Obs.OverlayHeight;
        try
        {
            var canvas = await _obsService.GetCanvasSizeAsync(cancellationToken);
            width = canvas.Width;
            height = canvas.Height;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            AddLog($"Luces virtuales OBS: no pude leer el tamano del canvas, uso la resolucion configurada. {ex.Message}", ActivityLogKind.Important);
        }

        var overlayUrl = _virtualLightsOverlayService.BuildActiveOverlayUrl(command, duration);
        var result = await _obsService.ShowBrowserSourceAsync(
            sceneName,
            NeoTwitchProduct.Obs.VirtualLightsSourceName,
            overlayUrl,
            width,
            height,
            cancellationToken);

        _currentVirtualLightsObsSceneName = sceneName;
        ApplyObsResult(result);
    }

    private async Task ClearVirtualLightsEffectAsync()
    {
        try
        {
            _virtualLightsOverlayService.ClearState();
            await _virtualLightsScreenOverlayService.HideAsync();

            var sceneName = _currentVirtualLightsObsSceneName;
            _currentVirtualLightsObsSceneName = "";
            if (!string.IsNullOrWhiteSpace(sceneName) && _config.Obs.IsConfigured)
            {
                if (!_obsService.IsConnected)
                {
                    await ConnectObsAsync();
                }

                if (_obsService.IsConnected)
                {
                    ApplyObsResult(await _obsService.HideSceneSourceAsync(
                        sceneName,
                        NeoTwitchProduct.Obs.VirtualLightsSourceName,
                        CancellationToken.None));
                }
            }
        }
        catch (Exception ex)
        {
            AddLog($"Luces virtuales: {ex.Message}", ActivityLogKind.Important);
        }
    }
}
