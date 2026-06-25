using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Lights;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Activity;
using WpfMessageBox = System.Windows.MessageBox;
using static NeoTwitch.Services.Text.UiTextFormatter;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void AddRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ResolvePendingRuleChanges())
        {
            return;
        }

        var rule = ConfigurationItemFactory.CreateRule();
        _config.Rules.Add(rule);
        ShowAllRuleFilters();
        RefreshRulesView();
        RulesList.SelectedItem = rule;
        SaveConfig();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
    }

    internal void DuplicateRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ResolvePendingRuleChanges())
        {
            return;
        }

        if (RulesList.SelectedItem is not EventRule rule)
        {
            return;
        }

        var copy = rule.Duplicate();
        _config.Rules.Add(copy);
        ShowAllRuleFilters();
        RefreshRulesView();
        RulesList.SelectedItem = copy;
        SaveConfig();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
    }

    internal void RemoveRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ResolvePendingRuleChanges())
        {
            return;
        }

        if (RulesList.SelectedItem is not EventRule rule)
        {
            return;
        }

        RemoveRule(rule);
    }

    private void RemoveRule(EventRule rule)
    {
        if (WpfMessageBox.Show(this, $"Eliminar la alerta '{rule.Name}'?", "Alertas", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        var wasSelected = ReferenceEquals(RulesList.SelectedItem, rule);
        _config.Rules.Remove(rule);
        RefreshRulesView();

        if (_config.Rules.Count > 0)
        {
            if (wasSelected || RulesList.SelectedItem is not EventRule)
            {
                RulesList.SelectedItem = _rulesViewSource.View?.Cast<EventRule>().FirstOrDefault();
            }
        }
        else
        {
            LoadSelectedRuleIntoUi();
        }

        SaveConfig();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
    }

    internal void SaveRuleButton_Click(object sender, RoutedEventArgs e)
    {
        SavePendingRuleChanges();
    }

    private void SavePendingRuleChanges()
    {
        if (!SaveCurrentRuleFromFields())
        {
            return;
        }

        SaveConfig();
        CaptureCurrentRuleSnapshot();
        SetRuleDirtyState(false);
        ScheduleTwitchSubscriptionRefreshIfNeeded();
        AddLog("Alerta guardada.");
    }

    private bool ResolvePendingRuleChanges()
    {
        if (!_hasUnsavedRuleChanges)
        {
            return true;
        }

        var ruleName = FirstNonEmpty(_editingRule?.Name ?? "", "esta alerta");
        var result = WpfMessageBox.Show(
            this,
            $"Hay cambios sin guardar en '{ruleName}'.\n\nQuieres guardarlos antes de continuar?",
            "Cambios sin guardar",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Cancel)
        {
            return false;
        }

        if (result == MessageBoxResult.Yes)
        {
            SavePendingRuleChanges();
            return true;
        }

        DiscardPendingRuleChanges();
        return true;
    }

    private void DiscardPendingRuleChanges()
    {
        var revertedRule = _editingRule;
        if (revertedRule is not null && _loadedRuleSnapshot is not null)
        {
            EventRuleSnapshotService.CopyValues(_loadedRuleSnapshot, revertedRule);
            RefreshRulesView();
            SaveConfig();
            if (ReferenceEquals(RulesList.SelectedItem, revertedRule))
            {
                LoadSelectedRuleIntoUi();
                return;
            }
        }

        SetRuleDirtyState(false);
    }

    private void CaptureCurrentRuleSnapshot()
    {
        _loadedRuleSnapshot = _editingRule is null
            ? null
            : EventRuleSnapshotService.Clone(_editingRule);
    }

    private void SetRuleDirtyState(bool isDirty)
    {
        _hasUnsavedRuleChanges = isDirty;

        if (SaveRuleButton is not null)
        {
            SaveRuleButton.Opacity = isDirty ? 1d : 0.68d;
            SaveRuleButton.ToolTip = isDirty
                ? "Hay cambios pendientes por guardar"
                : "No hay cambios pendientes";
        }
    }

    private void UpdateRuleDirtyStateFromSnapshot()
    {
        if (_editingRule is null || _loadedRuleSnapshot is null)
        {
            SetRuleDirtyState(false);
            return;
        }

        SetRuleDirtyState(!EventRuleSnapshotService.HaveSameEditableValues(_loadedRuleSnapshot, _editingRule));
    }

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

    internal void RuleAudioModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.Tag is not string value
            || !Enum.TryParse<AudioSourceMode>(value, out var mode))
        {
            return;
        }

        _ruleAudioMode = mode;
        UpdateRuleAudioModeSelection();
        UpdateRuleOptionVisibility();
        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }
    }

    internal void RuleObsMediaKindButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.Tag is not string value
            || !Enum.TryParse<ObsMediaKind>(value, out var kind))
        {
            return;
        }

        RuleObsMediaKindBox.SelectedValue = kind;
        RefreshRuleObsMediaChoices();
        UpdateRuleObsMediaModeSelection();
        UpdateRuleOptionVisibility();
        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }
    }

    internal void RuleObsMediaSourceModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.Tag is not string value
            || !Enum.TryParse<MediaSourceMode>(value, out var mode))
        {
            return;
        }

        RuleObsMediaSourceModeBox.SelectedValue = mode;
        UpdateRuleObsMediaModeSelection();
        UpdateRuleOptionVisibility();
        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }
    }

    internal void RulesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingComponent || _loadingUi || _suppressRuleSelectionChange)
        {
            return;
        }

        if (_hasUnsavedRuleChanges
            && _editingRule is not null
            && RulesList.SelectedItem is EventRule selected
            && !ReferenceEquals(selected, _editingRule))
        {
            if (!ResolvePendingRuleChanges())
            {
                try
                {
                    _suppressRuleSelectionChange = true;
                    RulesList.SelectedItem = _editingRule;
                }
                finally
                {
                    _suppressRuleSelectionChange = false;
                }

                return;
            }
        }

        LoadSelectedRuleIntoUi();
    }

    internal void StripsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        LoadSelectedStripIntoUi();
    }

    internal void RuleFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingRule)
        {
            return;
        }

        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }

        UpdateEventKindTileSelection();
        UpdatePatternTileSelection();
        UpdateRuleOptionVisibility();
        UpdateRuleLedPreviewTimerState();
    }

    internal void EventKindTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.Tag is not string value
            || !Enum.TryParse<TwitchEventKind>(value, out var kind))
        {
            return;
        }

        EventKindBox.SelectedValue = kind;
        UpdateEventKindTileSelection();
    }

    private async void StopTestButton_Click(object sender, RoutedEventArgs e)
    {
        await StopCurrentEffectAsync();
    }

}
