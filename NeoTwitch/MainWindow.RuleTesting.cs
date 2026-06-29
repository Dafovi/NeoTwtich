using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Activity;
using WpfMessageBox = System.Windows.MessageBox;
using static NeoTwitch.Services.Text.UiTextFormatter;

namespace NeoTwitch;

public partial class MainWindow
{
    internal async void RuleTestButton_Click(object sender, RoutedEventArgs e)
    {
        await ToggleRuleTestAsync();
    }

    private async void ToggleRuleTest()
    {
        await ToggleRuleTestAsync();
    }

    private async Task ToggleRuleTestAsync()
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
            CrashReporter.Log(ex, _text.Get(UiTextKeys.RuleTestFailureCrash));
            AddLog(_text.Format(UiTextKeys.RuleTestFailureLog, ex.Message), ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, _text.Get(UiTextKeys.RuleTestTitle), MessageBoxButton.OK, MessageBoxImage.Warning);
            UpdateRuleTestButtonState();
        }
    }

    private async Task StartRuleTestAsync()
    {
        if (_alertsViewModel.SelectedRule is not EventRule rule)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        if (!SaveCurrentRuleFromFields())
        {
            return;
        }

        var simulatedEvent = _ruleSimulation.BuildEvent(rule);

        if (!rule.Matches(simulatedEvent))
        {
            var message = _text.Format(
                UiTextKeys.RuleTestNoMatchPrompt,
                rule.Name,
                DisplayNameService.For(rule.EventKind, _text),
                DisplayNameService.For(simulatedEvent.Kind, _text));
            AddLog(_text.Format(UiTextKeys.RuleTestSimulatorLog, message), ActivityLogKind.Important);
            WpfMessageBox.Show(this, message, _text.Get(UiTextKeys.RuleTestSimulatorTitle), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!ValidateSimulatedRun(rule, simulatedEvent))
        {
            return;
        }

        AddLog(
            _text.Format(
                UiTextKeys.RuleTestRunningLog,
                _ruleSimulation.DescribeEvent(simulatedEvent),
                rule.Name,
                _ruleSimulation.DescribeActions(rule)),
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
            var message = _text.Format(UiTextKeys.RuleTestMissingAudioPrompt, rule.Name);
            AddLog(_text.Format(UiTextKeys.RuleTestSimulatorLog, message), ActivityLogKind.Important);
            WpfMessageBox.Show(this, message, _text.Get(UiTextKeys.RuleTestSimulatorTitle), MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (_config.ArduinoEnabled && rule.UseLights && !_lightController.HasOpenPort)
        {
            AddLog(
                string.IsNullOrWhiteSpace(_config.SerialPort)
                    ? _text.Get(UiTextKeys.RuleTestArduinoMissingComLog)
                    : _text.Format(UiTextKeys.RuleTestArduinoDisconnectedLog, _config.SerialPort),
                ActivityLogKind.Important);
        }

        if (_config.ArduinoEnabled && rule.UseLights && !string.IsNullOrWhiteSpace(rule.TargetPins) && LightCommand.ParsePins(rule.TargetPins).Count == 0)
        {
            AddLog(_text.Format(UiTextKeys.RuleTestInvalidPinsLog, rule.Name), ActivityLogKind.Important);
        }

        if (rule.SendAlexaEvent && !_config.Alexa.IsConfigured)
        {
            AddLog(_text.Get(UiTextKeys.RuleTestAlexaNotConfiguredLog), ActivityLogKind.Important);
        }

        if (rule.EventKind == TwitchEventKind.ChatCommand
            && !RuleSimulationService.MatchesChatCommand(rule, twitchEvent.Message))
        {
            AddLog(_text.Get(UiTextKeys.RuleTestChatCommandMismatchLog), ActivityLogKind.Important);
        }

        return true;
    }

    private async void StopTestButton_Click(object sender, RoutedEventArgs e)
    {
        await StopCurrentEffectAsync();
    }
}
