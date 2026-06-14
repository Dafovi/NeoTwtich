using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.ViewModels.Activity;
using WpfMessageBox = System.Windows.MessageBox;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void AddRuleButton_Click(object sender, RoutedEventArgs e)
    {
        var rule = new EventRule
        {
            Name = "Nueva regla",
            EventKind = TwitchEventKind.Follow,
            MinimumBits = 1,
            UseLights = false,
            PlayAudio = false,
            SendChatMessage = false,
            ChatMessageTemplate = "Gracias @{user}!"
        };

        _config.Rules.Add(rule);
        ShowAllRuleFilters();
        RefreshRulesView();
        RulesList.SelectedItem = rule;
        SaveConfig();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
    }

    internal void AddStripButton_Click(object sender, RoutedEventArgs e)
    {
        var nextPin = Enumerable.Range(2, 52)
            .FirstOrDefault(pin => _config.LedStrips.All(strip => strip.Pin != pin));

        var strip = new LedStripConfig
        {
            Name = "Nueva tira",
            Pin = nextPin == 0 ? 6 : nextPin,
            LedCount = 30
        };

        _config.LedStrips.Add(strip);
        StripsList.SelectedItem = strip;
        SaveConfig();
    }

    internal void DuplicateStripButton_Click(object sender, RoutedEventArgs e)
    {
        if (StripsList.SelectedItem is not LedStripConfig strip)
        {
            return;
        }

        var copy = strip.Duplicate();
        _config.LedStrips.Add(copy);
        StripsList.SelectedItem = copy;
        SaveConfig();
    }

    internal void RemoveStripButton_Click(object sender, RoutedEventArgs e)
    {
        if (StripsList.SelectedItem is not LedStripConfig strip)
        {
            return;
        }

        if (_config.LedStrips.Count == 1)
        {
            WpfMessageBox.Show(this, "Deja al menos una tira configurada.", "Luces de fondo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var index = StripsList.SelectedIndex;
        _config.LedStrips.Remove(strip);
        StripsList.SelectedIndex = Math.Clamp(index - 1, 0, _config.LedStrips.Count - 1);
        SaveConfig();
    }

    internal void DuplicateRuleButton_Click(object sender, RoutedEventArgs e)
    {
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
        SaveCurrentRuleFromFields();
        var simulatedEvent = BuildSimulatedEvent(rule);

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
            $"Simulando {DescribeSimulatedEvent(simulatedEvent)} para regla '{rule.Name}'. Acciones: {DescribeRuleActions(rule)}.",
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

    private TwitchEvent BuildSimulatedEvent(EventRule rule)
    {
        var kind = rule.EventKind == TwitchEventKind.Test
            ? TwitchEventKind.Follow
            : rule.EventKind;
        var userName = "Prueba";
        var bits = Math.Max(1, rule.MinimumBits);
        var viewers = 18;
        var rewardTitle = FirstNonEmpty(rule.CustomRewardTitle, "Canje de prueba");
        var message = kind == TwitchEventKind.ChatCommand
            ? FirstNonEmpty(rule.ChatCommand, "!baile mensaje de prueba")
            : "Mensaje de prueba";

        return new TwitchEvent
        {
            Kind = kind,
            UserName = userName,
            RewardTitle = kind == TwitchEventKind.ChannelPointRedemption ? rewardTitle : null,
            Bits = kind == TwitchEventKind.Cheer ? bits : null,
            ViewerCount = kind == TwitchEventKind.Raid ? viewers : null,
            Message = kind == TwitchEventKind.ChatCommand ? message : "Mensaje de prueba",
            RawType = "simulator",
            Title = $"Simulacion: {DisplayNames.For(kind)} de {userName}"
        };
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
            && !EventRuleMatchesChatCommand(rule, twitchEvent.Message))
        {
            AddLog("Simulador: el mensaje no empieza con el comando configurado.", ActivityLogKind.Important);
        }

        return true;
    }

    private static bool EventRuleMatchesChatCommand(EventRule rule, string? message)
    {
        if (rule.EventKind != TwitchEventKind.ChatCommand)
        {
            return true;
        }

        var command = rule.ChatCommand.Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        if (!command.StartsWith('!'))
        {
            command = $"!{command}";
        }

        var firstToken = message?.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.Equals(firstToken, command, StringComparison.OrdinalIgnoreCase);
    }

    private static string DescribeSimulatedEvent(TwitchEvent twitchEvent)
    {
        var user = FirstNonEmpty(twitchEvent.UserName ?? "", "Prueba");
        return twitchEvent.Kind switch
        {
            TwitchEventKind.Cheer => $"{twitchEvent.Bits ?? 0} bits de {user}",
            TwitchEventKind.Raid => $"raid de {user} con {twitchEvent.ViewerCount ?? 0} viewers",
            TwitchEventKind.ChannelPointRedemption => $"canje '{FirstNonEmpty(twitchEvent.RewardTitle ?? "", "Canje de prueba")}' de {user}",
            TwitchEventKind.ChatCommand => $"comando de chat de {user}: {FirstNonEmpty(twitchEvent.Message ?? "", "sin mensaje")}",
            _ => $"{DisplayNames.For(twitchEvent.Kind)} de {user}"
        };
    }

    private static string DescribeRuleActions(EventRule rule)
    {
        List<string> actions = [];

        if (rule.UseLights)
        {
            actions.Add("luces");
        }

        if (rule.PlayAudio)
        {
            actions.Add("audio");
        }

        if (rule.SendChatMessage)
        {
            actions.Add("chat");
        }

        if (rule.SendAlexaEvent)
        {
            actions.Add("Alexa");
        }

        return actions.Count == 0 ? "ninguna accion activa" : string.Join(", ", actions);
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
        SaveCurrentRuleFromFields();
        SaveConfig();
    }

    internal void RulesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
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

    internal void GlobalSettingsChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        SaveConfig();
        ApplyStartWithWindowsRegistration();
        UpdateSensitiveFieldVisibility();
        UpdateSliderLabels();
        UpdateStatusText();
        RefreshRulesView();
        UpdateRuleOptionVisibility();
        ApplyBackgroundOutputMode();
        UpdateCloseBehaviorCards();
    }

    internal void CloseBehaviorRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        CloseToTrayCheck.IsChecked = sender == CloseToTrayRadio;
        GlobalSettingsChanged(sender, e);
    }

    internal void AlexaSettingsChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        _alexaRelayConnected = false;
        SaveConfig();
        UpdateAlexaStatusText();
        UpdateSensitiveFieldVisibility();
        RefreshRulesView();
        UpdateRuleOptionVisibility();
    }

    internal async void TestAlexaButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isAlexaConnecting)
        {
            return;
        }

        try
        {
            _isAlexaConnecting = true;
            UpdateStatusText();
            SaveGlobalSettingsFromFields();
            SaveConfig();
            await _alexaRelayService.SendTestEventAsync(_config, CancellationToken.None);
            _alexaRelayConnected = true;
            AddLog("Alexa: evento de prueba enviado.", ActivityLogKind.Alexa);
        }
        catch (Exception ex)
        {
            _alexaRelayConnected = false;
            CrashReporter.Log(ex, "No se pudo enviar la prueba de Alexa.");
            AddLog($"Alexa: {ex.Message}", ActivityLogKind.Important);
            WpfMessageBox.Show(this, ex.Message, "Alexa", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _isAlexaConnecting = false;
            UpdateAlexaStatusText();
        }
    }

    internal void RuleFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingRule)
        {
            return;
        }

        SaveCurrentRuleFromFields();
        SaveConfig();
        UpdateEventKindTileSelection();
        UpdatePatternTileSelection();
        UpdateRuleOptionVisibility();
        UpdateRuleLedPreviewTimerState();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
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

    internal void PatternTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.Tag is not string value
            || !Enum.TryParse<LightPattern>(value, out var pattern))
        {
            return;
        }

        PatternBox.SelectedValue = pattern;
        UpdatePatternTileSelection();
        UpdateRuleLedPreviewFrame();
    }

    internal void BackgroundPatternTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.Tag is not string value
            || !Enum.TryParse<LightPattern>(value, out var pattern))
        {
            return;
        }

        BackgroundPatternBox.SelectedValue = pattern;
        UpdateBackgroundPatternTileSelection();
        UpdateBackgroundLedPreviewFrame();
    }

    internal void StripFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingStrip)
        {
            return;
        }

        SaveCurrentStripFromFields();
        SaveConfig();
        ScheduleBackgroundApply();
    }

    internal void BackgroundFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveBackgroundFromFields();
        SaveConfig();
        UpdateBackgroundOptionVisibility();
        UpdateBackgroundPatternTileSelection();
        UpdateBackgroundLedPreviewFrame();
        UpdateBackgroundLedPreviewTimerState();
    }

    internal void RuleLedPreviewPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateRuleLedPreviewTimerState();
    }

    internal void BackgroundLedPreviewPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateBackgroundLedPreviewTimerState();
    }

    internal void ThemeModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        ApplyTheme();
        SaveConfig();
        UpdateCloseBehaviorCards();
    }

    internal void ToggleClientIdVisibility_Click(object sender, RoutedEventArgs e)
    {
        _showClientId = !_showClientId;
        UpdateSensitiveFieldVisibility();
    }

    internal void ToggleClientSecretVisibility_Click(object sender, RoutedEventArgs e)
    {
        _showClientSecret = !_showClientSecret;
        UpdateSensitiveFieldVisibility();
    }

    internal void ToggleAlexaRelayUrlVisibility_Click(object sender, RoutedEventArgs e)
    {
        _showAlexaRelayUrl = !_showAlexaRelayUrl;
        UpdateSensitiveFieldVisibility();
    }

    internal void ToggleAlexaAuthTokenVisibility_Click(object sender, RoutedEventArgs e)
    {
        _showAlexaAuthToken = !_showAlexaAuthToken;
        UpdateSensitiveFieldVisibility();
    }

    internal void ToggleObsPasswordVisibility_Click(object sender, RoutedEventArgs e)
    {
        _showObsPassword = !_showObsPassword;
        UpdateSensitiveFieldVisibility();
    }

    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, MainTabs) || _initializingComponent)
        {
            return;
        }

        UpdateNavigationButtons();
        if (int.TryParse(NavAudioButton.Tag?.ToString(), out var audioTabIndex)
            && MainTabs.SelectedIndex != audioTabIndex)
        {
            StopAudioPreview();
        }

        UpdateRuleLedPreviewTimerState();
        UpdateBackgroundLedPreviewTimerState();
        ConfigureActionIcons();
        _ = Dispatcher.BeginInvoke(ApplyTheme, DispatcherPriority.Loaded);
        _ = Dispatcher.BeginInvoke(ApplyTheme, DispatcherPriority.ContextIdle);
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string tag }
            || !int.TryParse(tag, out var selectedIndex)
            || selectedIndex < 0
            || selectedIndex >= MainTabs.Items.Count)
        {
            return;
        }

        MainTabs.SelectedIndex = selectedIndex;
        UpdateNavigationButtons();
    }

    internal void GoToActivityButton_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(NavActivityButton.Tag?.ToString(), out var activityTabIndex))
        {
            MainTabs.SelectedIndex = activityTabIndex;
        }

        UpdateNavigationButtons();
    }

    private async void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        await ExitApplicationAsync();
    }

    internal void PrimaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(PrimaryColorBox);
    }

    internal void SecondaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(SecondaryColorBox);
    }

    internal void TertiaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(TertiaryColorBox);
    }

    internal void BackgroundPrimaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(BackgroundPrimaryColorBox);
    }

    internal void BackgroundSecondaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(BackgroundSecondaryColorBox);
    }

    internal void BackgroundTertiaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(BackgroundTertiaryColorBox);
    }

    internal async void ApplyArduinoBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SaveGlobalSettingsFromFields();
        SaveCurrentStripFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        await ApplyArduinoBackgroundAsync();
    }

    internal async void ApplyAlexaBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SaveGlobalSettingsFromFields();
        SaveBackgroundFromFields();
        SaveConfig();

        if (_config.BackgroundAlexaEnabled)
        {
            await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOnEventName, "Fondo Alexa encendido", force: true);
        }
    }

    internal async void StopArduinoBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SaveGlobalSettingsFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
    }

    internal async void StopAlexaBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SaveGlobalSettingsFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOffEventName, "Fondo Alexa apagado", force: true);
    }

    private async void StopTestButton_Click(object sender, RoutedEventArgs e)
    {
        await StopCurrentEffectAsync();
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        _activity.Clear();
    }

    internal void RuleSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingUi || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        _ruleSearchText = textBox.Text.Trim();
        RefreshRulesView();
    }

    internal void RuleStatusFilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (_loadingUi || sender is not ToggleButton button)
        {
            return;
        }

        button.IsChecked = true;
        _ruleStatusFilter = button.Tag?.ToString() ?? "ALL";

        foreach (var filterButton in RuleStatusFilterButtons())
        {
            if (!ReferenceEquals(filterButton, button))
            {
                filterButton.IsChecked = false;
            }

            ApplyRuleStatusFilterButtonTheme(filterButton, _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light);
        }

        RefreshRulesView();
    }

    internal void RuleCategoryFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingUi)
        {
            return;
        }

        _ruleCategoryFilter = RuleCategoryFilterBox.SelectedValue?.ToString() ?? "";
        RefreshRulesView();
    }

    private void RulesViewSource_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not EventRule rule)
        {
            e.Accepted = false;
            return;
        }

        e.Accepted = RuleMatchesFilters(rule);
    }

    private bool RuleMatchesFilters(EventRule rule)
    {
        if (_ruleStatusFilter == "ACTIVE" && !rule.IsEnabled)
        {
            return false;
        }

        if (_ruleStatusFilter == "INACTIVE" && rule.IsEnabled)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_ruleCategoryFilter)
            && !string.Equals(rule.EventKind.ToString(), _ruleCategoryFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_ruleSearchText))
        {
            return true;
        }

        var text = _ruleSearchText;
        return ContainsIgnoreCase(rule.Name, text)
            || ContainsIgnoreCase(rule.DisplayLabel, text)
            || ContainsIgnoreCase(rule.ChatCommand, text)
            || ContainsIgnoreCase(rule.CustomRewardTitle, text)
            || ContainsIgnoreCase(rule.ChatMessageTemplate, text)
            || ContainsIgnoreCase(DisplayNames.For(rule.EventKind), text);
    }

    private static bool ContainsIgnoreCase(string? value, string query)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshRulesView()
    {
        UpdateRuleExternalActionAvailability();
        var selected = RulesList.SelectedItem as EventRule;
        _rulesViewSource.View?.Refresh();

        if (selected is not null && _rulesViewSource.View?.Contains(selected) == true)
        {
            RulesList.SelectedItem = selected;
        }
        else if (RulesList.SelectedItem is not EventRule)
        {
            RulesList.SelectedItem = _rulesViewSource.View?.Cast<EventRule>().FirstOrDefault();
        }

        UpdateRulesCountText();
    }

    private void UpdateRuleExternalActionAvailability()
    {
        if (_config.Rules.Count == 0)
        {
            return;
        }

        var lightsAvailable = _config.ArduinoEnabled;
        var alexaAvailable = _config.Alexa.IsConfigured;
        var obsAvailable = _config.Obs.IsConfigured;

        foreach (var rule in _config.Rules)
        {
            rule.LightsActionAvailable = lightsAvailable;
            rule.AlexaActionAvailable = alexaAvailable;
            rule.ObsActionAvailable = obsAvailable;
        }
    }

    private void UpdateRulesCountText()
    {
        if (_initializingComponent || RulesCountText is null)
        {
            return;
        }

        var visibleCount = _rulesViewSource.View?.Cast<EventRule>().Count() ?? 0;
        RulesCountText.Text = $"Mostrando {visibleCount} de {_config.Rules.Count} alertas";
    }

    private void ShowAllRuleFilters()
    {
        _ruleStatusFilter = "ALL";
        _ruleCategoryFilter = "";
        RuleFilterAllButton.IsChecked = true;
        RuleFilterActiveButton.IsChecked = false;
        RuleFilterInactiveButton.IsChecked = false;
        RuleCategoryFilterBox.SelectedValue = "";
        RuleSearchBox.Text = "";
        _ruleSearchText = "";
    }
}
