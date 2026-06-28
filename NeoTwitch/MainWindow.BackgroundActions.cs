using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Lights;
using NeoTwitch.Services.Text;
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
            await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOnEventName, _text.Get(UiTextKeys.BackgroundAlexaOnTitle));
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
                AddLog(_text.Get(UiTextKeys.BackgroundMissingComLog));
                return;
            }

            try
            {
                await ConnectArduinoAsync();
            }
            catch (Exception ex)
            {
                CrashReporter.Log(ex, _text.Format(UiTextKeys.BackgroundArduinoConnectFailureCrash, _config.SerialPort));
                AddLog(_text.Format(UiTextKeys.BackgroundArduinoConnectFailureLog, _config.SerialPort), ActivityLogKind.Important);
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
            AddLog(_text.Format(UiTextKeys.BackgroundAppliedLog, DisplayNameService.For(command.Pattern, _text)));
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
            await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOffEventName, _text.Get(UiTextKeys.BackgroundAlexaOffTitle));
        }
        else if (_config.BackgroundAlexaEnabled)
        {
            await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOnEventName, _text.Get(UiTextKeys.BackgroundAlexaOnTitle));
        }
    }

    private async Task RestoreArduinoBackgroundStateWithRetriesAsync(bool retryArduino)
    {
        var attempts = BackgroundLightRestoreService.ResolveArduinoRestoreAttempts(
            _config.ArduinoEnabled,
            _config.BackgroundEnabled,
            retryArduino);

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
            AddLog(_text.Format(UiTextKeys.BackgroundAlexaSentLog, eventName), ActivityLogKind.Alexa);
        }
        catch (Exception ex)
        {
            _alexaRelayConnected = false;
            CrashReporter.Log(ex, _text.Format(UiTextKeys.BackgroundAlexaFailureCrash, eventName));
            AddLog(_text.Format(UiTextKeys.BackgroundAlexaFailureLog, ex.Message), ActivityLogKind.Important);
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
                CrashReporter.Log(ex, _text.Get(UiTextKeys.BackgroundScheduledFailureCrash));
                AddLog(_text.Format(UiTextKeys.BackgroundScheduledFailureLog, ex.Message));
            }
        });
    }
}
