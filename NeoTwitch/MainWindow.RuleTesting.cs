using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Activity;
using WpfMessageBox = System.Windows.MessageBox;
using static NeoTwitch.Services.Text.UiTextFormatter;

namespace NeoTwitch;

public partial class MainWindow
{
    internal async void RuleTestButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentEffectCts is not null)
            {
                await StopCurrentEffectAsync();
                UpdateRuleTestButtonState();
                return;
            }

            await StartRuleTestAsync();
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, "No se pudo probar la alerta.");
            AddLog($"Prueba de alerta: {ex.Message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "Probar alerta", MessageBoxButton.OK, MessageBoxImage.Warning);
            UpdateRuleTestButtonState();
        }
    }

    private async Task StartRuleTestAsync()
    {
        if (RulesList.SelectedItem is not EventRule rule)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        if (!SaveCurrentRuleFromFields())
        {
            return;
        }

        var simulatedEvent = RuleSimulationService.BuildEvent(rule);

        if (!rule.Matches(simulatedEvent))
        {
            var message = $"La regla '{rule.Name}' no se ejecutaria con esta simulacion. Regla: {DisplayNames.For(rule.EventKind)}. Simulacion: {DisplayNames.For(simulatedEvent.Kind)}.";
            AddLog($"Simulador: {message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, message, "Simulador de eventos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!ValidateSimulatedRun(rule, simulatedEvent))
        {
            return;
        }

        AddLog(
            $"Simulando {RuleSimulationService.DescribeEvent(simulatedEvent)} para regla '{rule.Name}'. Acciones: {RuleSimulationService.DescribeActions(rule)}.",
            ActivityLogKind.Event);

        await RunRuleAsync(rule, simulatedEvent);
    }

    private void UpdateRuleTestButtonState()
    {
        if (_initializingComponent)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateRuleTestButtonState);
            return;
        }

        var isRunning = _currentEffectCts is not null;
        RuleTestButton.Style = isRunning
            ? TryFindResource("DangerButton") as Style
            : TryFindResource("PrimaryButton") as Style;
        SetButtonIcon(RuleTestButton, isRunning ? "Parar prueba" : "Probar alerta", isRunning ? "Square" : "Play");
        ApplyButtonTheme(RuleTestButton, _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light);
    }

    private bool ValidateSimulatedRun(EventRule rule, TwitchEvent twitchEvent)
    {
        if (rule.PlayAudio && !RuleHasValidAudio(rule))
        {
            var message = $"El audio de '{rule.Name}' no existe o no esta configurado.";
            AddLog($"Simulador: {message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, message, "Simulador de eventos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (_config.ArduinoEnabled && rule.UseLights && !_lightController.HasOpenPort)
        {
            AddLog(
                string.IsNullOrWhiteSpace(_config.SerialPort)
                    ? "Simulador: la regla usa luces, pero no hay puerto COM configurado."
                    : $"Simulador: la regla usa luces, pero Arduino no esta conectado ahora ({_config.SerialPort}).",
                ActivityLogKind.Important);
        }

        if (_config.ArduinoEnabled && rule.UseLights && !string.IsNullOrWhiteSpace(rule.TargetPins) && LightCommand.ParsePins(rule.TargetPins).Count == 0)
        {
            AddLog($"Simulador: los pines de la regla '{rule.Name}' no son validos.", ActivityLogKind.Important);
        }

        if (rule.SendAlexaEvent && !_config.Alexa.IsConfigured)
        {
            AddLog("Simulador: Alexa esta activada en la regla, pero el relay no esta configurado.", ActivityLogKind.Important);
        }

        if (rule.EventKind == TwitchEventKind.ChatCommand
            && !RuleSimulationService.MatchesChatCommand(rule, twitchEvent.Message))
        {
            AddLog("Simulador: el mensaje no empieza con el comando configurado.", ActivityLogKind.Important);
        }

        return true;
    }

    private async void StopTestButton_Click(object sender, RoutedEventArgs e)
    {
        await StopCurrentEffectAsync();
    }
}
