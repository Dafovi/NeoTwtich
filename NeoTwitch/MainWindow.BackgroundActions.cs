using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    private async Task ApplyBackgroundAsync()
    {
        if (!_config.BackgroundEnabled && !_config.BackgroundAlexaEnabled)
        {
            return;
        }

        if (_config.ArduinoEnabled && _config.BackgroundEnabled)
        {
            await ApplyArduinoBackgroundAsync();
        }

        if (_config.BackgroundAlexaEnabled)
        {
            await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOnEventName, "Fondo Alexa encendido");
        }
    }

    private async Task ApplyArduinoBackgroundAsync()
    {
        if (!_config.ArduinoEnabled || !_config.BackgroundEnabled)
        {
            return;
        }

        if (!_lightController.HasOpenPort)
        {
            if (string.IsNullOrWhiteSpace(_config.SerialPort))
            {
                AddLog("No puedo aplicar fondo sin puerto COM.");
                return;
            }

            try
            {
                await ConnectArduinoAsync();
            }
            catch (Exception ex)
            {
                CrashReporter.Log(ex, $"No se pudo conectar Arduino para aplicar fondo en {_config.SerialPort}.");
                AddLog($"Arduino: no pude aplicar fondo en {_config.SerialPort}. Revisa el puerto y conecta manualmente.", ActivityLogKind.Important);
                UpdateStatusText();
                return;
            }
        }

        if (_lightController.HasOpenPort)
        {
            await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
            await Task.Delay(LightStopSettleMs);

            var command = LightCommand.FromBackground(_config);
            await _lightController.SendAsync(command, AddLog, CancellationToken.None);
            UpdateStatusText();
            AddLog($"Fondo aplicado: {DisplayNames.For(command.Pattern)}.");
        }
    }

    private async Task ApplyBackgroundStateAsync()
    {
        if (_effectGate.CurrentCount == 0)
        {
            return;
        }

        await RestoreBackgroundStateAsync(retryArduino: false);
    }

    private async Task RestoreBackgroundStateAsync(bool retryArduino = true)
    {
        await RestoreArduinoBackgroundStateWithRetriesAsync(retryArduino);

        if (_config.BackgroundAlexaTurnOffAfterEvent)
        {
            await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOffEventName, "Fondo Alexa apagado");
        }
        else if (_config.BackgroundAlexaEnabled)
        {
            await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOnEventName, "Fondo Alexa encendido");
        }
    }

    private async Task RestoreArduinoBackgroundStateWithRetriesAsync(bool retryArduino)
    {
        var attempts = _config.ArduinoEnabled && _config.BackgroundEnabled && retryArduino ? 2 : 1;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            if (_config.ArduinoEnabled && _config.BackgroundEnabled)
            {
                await ApplyArduinoBackgroundAsync();
            }
            else if (_config.ArduinoEnabled)
            {
                await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
            }

            if (attempt < attempts)
            {
                await Task.Delay(180);
            }
        }
    }

    private async Task SendBackgroundAlexaEventAsync(string eventName, string title, bool force = false)
    {
        if (!_config.Alexa.IsConfigured
            || (!force && !_config.BackgroundAlexaEnabled && !_config.BackgroundAlexaTurnOffAfterEvent))
        {
            return;
        }

        try
        {
            await _alexaRelayService.SendBackgroundEventAsync(_config, eventName, title, CancellationToken.None);
            _alexaRelayConnected = true;
            AddLog($"Alexa fondo: {eventName}.", ActivityLogKind.Alexa);
        }
        catch (Exception ex)
        {
            _alexaRelayConnected = false;
            CrashReporter.Log(ex, $"No se pudo enviar fondo Alexa '{eventName}'.");
            AddLog($"Alexa fondo: {ex.Message}", ActivityLogKind.Important);
        }
        finally
        {
            UpdateAlexaStatusText();
        }
    }

    private void ScheduleBackgroundApply()
    {
        _backgroundApplyDebounce?.Cancel();
        _backgroundApplyDebounce?.Dispose();

        var cts = new CancellationTokenSource();
        _backgroundApplyDebounce = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(450, cts.Token);
                var operation = Dispatcher.InvokeAsync(ApplyBackgroundStateAsync);
                await await operation.Task;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                CrashReporter.Log(ex, "No se pudo aplicar el fondo programado.");
                AddLog($"Fondo: {ex.Message}");
            }
        });
    }
}
