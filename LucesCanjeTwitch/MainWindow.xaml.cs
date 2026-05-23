using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LucesCanjeTwitch.Models;
using LucesCanjeTwitch.Services;
using Forms = System.Windows.Forms;
using WpfClipboard = System.Windows.Clipboard;
using WpfMessageBox = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace LucesCanjeTwitch;

public partial class MainWindow : Window
{
    private readonly SettingsStore _settingsStore = new();
    private readonly AudioPlayerService _audioPlayer = new();
    private readonly SerialLightController _lightController = new();
    private readonly TwitchAuthService _authService = new();
    private readonly TwitchEventSubClient _eventSubClient;
    private readonly ObservableCollection<string> _activity = [];
    private readonly SemaphoreSlim _effectGate = new(1, 1);
    private IReadOnlyList<SerialPortInfo> _availablePorts = [];
    private readonly UiOption<TwitchEventKind>[] _eventOptions =
    [
        new("Nuevo seguidor", TwitchEventKind.Follow),
        new("Nueva suscripcion", TwitchEventKind.Subscription),
        new("Raid recibida", TwitchEventKind.Raid),
        new("Canje de puntos", TwitchEventKind.ChannelPointRedemption),
        new("Prueba manual", TwitchEventKind.Test)
    ];
    private readonly UiOption<LightPattern>[] _patternOptions =
    [
        new("Color fijo", LightPattern.Solid),
        new("Pulso", LightPattern.Pulse),
        new("Arcoiris", LightPattern.Rainbow),
        new("Carrera", LightPattern.Chase),
        new("Teatro", LightPattern.Theater),
        new("Destellos", LightPattern.Sparkle),
        new("Rave", LightPattern.Rave)
    ];

    private AppConfig _config = AppConfig.CreateDefault();
    private bool _initializingComponent;
    private bool _loadingUi;
    private bool _loadingRule;
    private bool _loadingStrip;
    private bool _isExiting;
    private CancellationTokenSource? _backgroundApplyDebounce;
    private CancellationTokenSource? _currentEffectCts;
    private AudioPlayback? _currentPlayback;
    private Forms.NotifyIcon? _notifyIcon;

    public MainWindow()
    {
        _config = _settingsStore.Load();

        try
        {
            _initializingComponent = true;
            InitializeComponent();
        }
        finally
        {
            _initializingComponent = false;
        }

        _eventSubClient = new TwitchEventSubClient(_authService, () => _config, SaveConfig, AddLog);
        _eventSubClient.EventReceived += EventSubClient_EventReceived;

        ActivityList.ItemsSource = _activity;
        EventKindBox.ItemsSource = _eventOptions;
        EventKindBox.DisplayMemberPath = nameof(UiOption<TwitchEventKind>.Label);
        EventKindBox.SelectedValuePath = nameof(UiOption<TwitchEventKind>.Value);
        PatternBox.ItemsSource = _patternOptions;
        PatternBox.DisplayMemberPath = nameof(UiOption<LightPattern>.Label);
        PatternBox.SelectedValuePath = nameof(UiOption<LightPattern>.Value);
        BackgroundPatternBox.ItemsSource = _patternOptions;
        BackgroundPatternBox.DisplayMemberPath = nameof(UiOption<LightPattern>.Label);
        BackgroundPatternBox.SelectedValuePath = nameof(UiOption<LightPattern>.Value);
        StripsList.ItemsSource = _config.LedStrips;
        PortComboBox.DisplayMemberPath = nameof(SerialPortInfo.DisplayName);
        PortComboBox.SelectedValuePath = nameof(SerialPortInfo.PortName);
        RefreshPortList(choosePreferred: false);

        CreateTrayIcon();
        LoadConfigIntoUi();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        AddLog("Aplicacion lista.");
        AddLog($"Configuracion: {_settingsStore.SettingsPath}");
        AddLog($"Log de errores: {CrashReporter.PreferredLogPath}");
        if (!string.IsNullOrWhiteSpace(_settingsStore.LastLoadError))
        {
            AddLog($"No pude leer la configuracion anterior: {_settingsStore.LastLoadError}");
        }

        if (_config.StartHidden)
        {
            Hide();
        }

        if (_config.AutoConnectArduino && !string.IsNullOrWhiteSpace(_config.SerialPort))
        {
            await ConnectArduinoAsync();
            await ApplyBackgroundAsync();
        }

        if (_config.AutoConnectTwitch && _config.Token.HasToken)
        {
            await StartTwitchAsync();
        }
    }

    private async void TwitchButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveGlobalSettingsFromFields();

            if (_eventSubClient.IsRunning)
            {
                await _eventSubClient.StopAsync();
                AddLog("Twitch desconectado.");
                UpdateStatusText();
                return;
            }

            if (!_config.Token.HasToken)
            {
                await SignInToTwitchAsync();
            }

            await StartTwitchAsync();
        }
        catch (Exception ex)
        {
            AddLog($"Twitch: {ex.Message}");
            WpfMessageBox.Show(this, ex.Message, "Twitch", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task SignInToTwitchAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.TwitchClientId))
        {
            throw new InvalidOperationException("Escribe primero el Client ID de Twitch.");
        }

        TwitchButton.IsEnabled = false;
        TwitchStatusText.Text = "Esperando autorizacion...";

        try
        {
            var session = await _authService.BeginDeviceFlowAsync(_config.TwitchClientId, CancellationToken.None);
            WpfClipboard.SetText(session.UserCode);
            _authService.OpenVerificationPage(session);
            WpfMessageBox.Show(
                this,
                $"Autoriza la app en Twitch con el codigo {session.UserCode}. El codigo ya quedo copiado al portapapeles.",
                "Login Twitch",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _config.Token = await _authService.PollForTokenAsync(_config.TwitchClientId, session, AddLog, CancellationToken.None);
            _config.Channel = await _authService.GetCurrentUserAsync(_config, CancellationToken.None);
            SaveConfig();
            AddLog($"Twitch autorizado como {_config.Channel.DisplayName}.");
        }
        finally
        {
            TwitchButton.IsEnabled = true;
            UpdateStatusText();
        }
    }

    private async Task StartTwitchAsync()
    {
        await _authService.EnsureValidTokenAsync(_config, AddLog, CancellationToken.None);

        if (!_config.Channel.IsReady)
        {
            _config.Channel = await _authService.GetCurrentUserAsync(_config, CancellationToken.None);
            SaveConfig();
        }

        await _eventSubClient.StartAsync();
        AddLog("Twitch escuchando eventos.");
        UpdateStatusText();
    }

    private async void ConnectArduinoButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveGlobalSettingsFromFields();
            await ConnectArduinoAsync();
            await ApplyBackgroundAsync();
        }
        catch (Exception ex)
        {
            AddLog($"Arduino: {ex.Message}");
            WpfMessageBox.Show(this, ex.Message, "Arduino", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task ConnectArduinoAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.SerialPort))
        {
            AddLog("No hay puerto COM configurado.");
            return;
        }

        await _lightController.ConfigureAsync(_config.SerialPort, _config.BaudRate, AddLog, CancellationToken.None);
        UpdateStatusText();
    }

    private async Task ApplyBackgroundAsync()
    {
        if (!_config.BackgroundEnabled)
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

            await ConnectArduinoAsync();
        }

        var command = LightCommand.FromBackground(_config);
        await _lightController.SendAsync(command, AddLog, CancellationToken.None);
        AddLog($"Fondo aplicado: {DisplayNames.For(command.Pattern)}.");
    }

    private async Task ApplyBackgroundStateAsync()
    {
        if (_effectGate.CurrentCount == 0)
        {
            return;
        }

        if (_config.BackgroundEnabled)
        {
            await ApplyBackgroundAsync();
            return;
        }

        await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
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
        });
    }

    private void RefreshPortList(bool choosePreferred)
    {
        var previousPort = ParsePort(PortComboBox.Text);

        try
        {
            _availablePorts = SerialLightController.GetAvailablePortInfos();
            PortComboBox.ItemsSource = _availablePorts;
        }
        catch (Exception ex)
        {
            _availablePorts = [];
            PortComboBox.ItemsSource = _availablePorts;
            CrashReporter.Log(ex, "No se pudo refrescar la lista de puertos COM.");
            AddLog($"No pude refrescar los puertos COM: {ex.Message}");
        }

        var selectedPort = choosePreferred
            ? ChoosePreferredPort(_availablePorts)
            : _config.SerialPort;

        if (string.IsNullOrWhiteSpace(selectedPort))
        {
            selectedPort = previousPort;
        }

        if (!string.IsNullOrWhiteSpace(selectedPort))
        {
            PortComboBox.SelectedValue = selectedPort;
            PortComboBox.Text = selectedPort;
        }
    }

    private async Task StopLightsAsync(IReadOnlyList<LightStripTarget> targets)
    {
        if (!_lightController.HasOpenPort)
        {
            return;
        }

        await _lightController.StopAsync(targets, AddLog, CancellationToken.None);
    }

    private void DetectPortsButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshPortList(choosePreferred: true);
        if (_availablePorts.Count == 0)
        {
            AddLog("No encontre puertos COM disponibles.");
            return;
        }

        AddLog($"Puertos detectados: {string.Join(", ", _availablePorts.Select(port => port.DisplayName))}");
    }

    private void PortComboBox_DropDownOpened(object sender, EventArgs e)
    {
        RefreshPortList(choosePreferred: false);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveGlobalSettingsFromFields();
        SaveCurrentRuleFromFields();
        SaveCurrentStripFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        await ApplyBackgroundStateAsync();
        AddLog("Configuracion guardada.");
    }

    private void AddRuleButton_Click(object sender, RoutedEventArgs e)
    {
        var rule = new EventRule
        {
            Name = "Nueva regla",
            EventKind = TwitchEventKind.Follow,
            UseLights = true,
            PlayAudio = false
        };

        _config.Rules.Add(rule);
        RulesList.SelectedItem = rule;
        SaveConfig();
    }

    private void AddStripButton_Click(object sender, RoutedEventArgs e)
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

    private void DuplicateStripButton_Click(object sender, RoutedEventArgs e)
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

    private void RemoveStripButton_Click(object sender, RoutedEventArgs e)
    {
        if (StripsList.SelectedItem is not LedStripConfig strip)
        {
            return;
        }

        if (_config.LedStrips.Count == 1)
        {
            WpfMessageBox.Show(this, "Deja al menos una tira configurada.", "Tiras LED", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var index = StripsList.SelectedIndex;
        _config.LedStrips.Remove(strip);
        StripsList.SelectedIndex = Math.Clamp(index - 1, 0, _config.LedStrips.Count - 1);
        SaveConfig();
    }

    private void DuplicateRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (RulesList.SelectedItem is not EventRule rule)
        {
            return;
        }

        var copy = rule.Duplicate();
        _config.Rules.Add(copy);
        RulesList.SelectedItem = copy;
        SaveConfig();
    }

    private void RemoveRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (RulesList.SelectedItem is not EventRule rule)
        {
            return;
        }

        if (WpfMessageBox.Show(this, $"Eliminar la regla '{rule.Name}'?", "Reglas", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        var index = RulesList.SelectedIndex;
        _config.Rules.Remove(rule);
        if (_config.Rules.Count > 0)
        {
            RulesList.SelectedIndex = Math.Clamp(index - 1, 0, _config.Rules.Count - 1);
        }
        else
        {
            LoadSelectedRuleIntoUi();
        }

        SaveConfig();
    }

    private async void TestRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (RulesList.SelectedItem is not EventRule rule)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        SaveCurrentRuleFromFields();
        await RunRuleAsync(rule, new TwitchEvent
        {
            Kind = rule.EventKind,
            RewardTitle = rule.CustomRewardTitle,
            UserName = "Prueba",
            Title = $"Prueba de {rule.Name}"
        });
    }

    private void BrowseAudioButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Filter = "Audio|*.wav;*.mp3;*.wma;*.aac;*.m4a|Todos los archivos|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        AudioPathBox.Text = dialog.FileName;
        SaveCurrentRuleFromFields();
    }

    private void RulesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        LoadSelectedRuleIntoUi();
    }

    private void StripsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        LoadSelectedStripIntoUi();
    }

    private void GlobalSettingsChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        SaveConfig();
        UpdateStatusText();
    }

    private void RuleFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingRule)
        {
            return;
        }

        SaveCurrentRuleFromFields();
        SaveConfig();
    }

    private void StripFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingStrip)
        {
            return;
        }

        SaveCurrentStripFromFields();
        SaveConfig();
    }

    private void BackgroundFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveBackgroundFromFields();
        SaveConfig();
        ScheduleBackgroundApply();
    }

    private void ThemeModeChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        ApplyTheme();
        SaveConfig();
    }

    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, MainTabs) || _initializingComponent)
        {
            return;
        }

        _ = Dispatcher.BeginInvoke(ApplyTheme);
    }

    private void PrimaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(PrimaryColorBox);
    }

    private void SecondaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(SecondaryColorBox);
    }

    private void TertiaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(TertiaryColorBox);
    }

    private void BackgroundPrimaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(BackgroundPrimaryColorBox);
    }

    private void BackgroundSecondaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(BackgroundSecondaryColorBox);
    }

    private void BackgroundTertiaryColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(BackgroundTertiaryColorBox);
    }

    private async void ApplyBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SaveGlobalSettingsFromFields();
        SaveCurrentStripFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        await ApplyBackgroundAsync();
    }

    private async void StopLightsButton_Click(object sender, RoutedEventArgs e)
    {
        await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
    }

    private async void StopTestButton_Click(object sender, RoutedEventArgs e)
    {
        await StopCurrentEffectAsync();
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        _activity.Clear();
    }

    private async void EventSubClient_EventReceived(TwitchEvent twitchEvent)
    {
        AddLog(twitchEvent.Title);

        var matchingRules = _config.Rules.Where(rule => rule.Matches(twitchEvent)).ToArray();
        if (matchingRules.Length == 0)
        {
            AddLog("El evento no coincide con reglas activas.");
            return;
        }

        foreach (var rule in matchingRules)
        {
            await RunRuleAsync(rule, twitchEvent);
        }
    }

    private async Task RunRuleAsync(EventRule rule, TwitchEvent twitchEvent)
    {
        await _effectGate.WaitAsync();
        var effectCts = new CancellationTokenSource();
        _currentEffectCts = effectCts;
        var wasCancelled = false;

        try
        {
            AudioPlayback? playback = null;
            if (rule.PlayAudio)
            {
                playback = await _audioPlayer.PrepareAsync(rule.AudioPath, AddLog);
                _currentPlayback = playback;
            }

            if (!rule.UseLights)
            {
                playback?.Play();
                if (playback is not null)
                {
                    await playback.Completion.WaitAsync(effectCts.Token);
                }

                return;
            }

            if (!_lightController.HasOpenPort && !string.IsNullOrWhiteSpace(_config.SerialPort))
            {
                await ConnectArduinoAsync();
            }

            var targets = LightCommand.ResolveTargets(_config, rule.TargetPins);
            await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
            await Task.Delay(40);

            var audioDuration = playback?.Duration;
            var syncedDurationMs = audioDuration is { TotalMilliseconds: > 0 }
                ? (int)Math.Round(audioDuration.Value.TotalMilliseconds)
                : (int?)null;

            var command = LightCommand.FromRule(rule, _config, syncedDurationMs);
            await _lightController.SendAsync(command, AddLog, CancellationToken.None);
            playback?.Play();

            if (playback is not null)
            {
                await playback.Completion.WaitAsync(effectCts.Token);
            }
            else
            {
                await Task.Delay(command.DurationMs, effectCts.Token);
            }

            await StopLightsAsync(targets);
            await ApplyBackgroundAsync();
            AddLog($"Luces: {DisplayNames.For(rule.Pattern)} por {command.DurationMs} ms para {DisplayNames.For(twitchEvent.Kind)}.");
        }
        catch (OperationCanceledException)
        {
            wasCancelled = true;
            AddLog("Prueba detenida.");
        }
        finally
        {
            _currentPlayback = null;
            if (ReferenceEquals(_currentEffectCts, effectCts))
            {
                _currentEffectCts = null;
            }

            effectCts.Dispose();
            _effectGate.Release();

            if (wasCancelled)
            {
                await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
                await ApplyBackgroundStateAsync();
            }
        }
    }

    private async Task StopCurrentEffectAsync()
    {
        _currentEffectCts?.Cancel();
        _currentPlayback?.Stop();
        await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));

        if (_effectGate.CurrentCount > 0)
        {
            await ApplyBackgroundStateAsync();
        }
    }

    private void LoadConfigIntoUi()
    {
        _loadingUi = true;

        try
        {
            ClientIdBox.Text = _config.TwitchClientId;
            PortComboBox.SelectedValue = _config.SerialPort;
            PortComboBox.Text = _config.SerialPort;
            BaudRateBox.Text = _config.BaudRate.ToString();
            AutoTwitchCheck.IsChecked = _config.AutoConnectTwitch;
            AutoArduinoCheck.IsChecked = _config.AutoConnectArduino;
            StartHiddenCheck.IsChecked = _config.StartHidden;
            DarkModeCheck.IsChecked = _config.DarkMode;
            BackgroundEnabledCheck.IsChecked = _config.BackgroundEnabled;
            BackgroundPinsBox.Text = _config.BackgroundTargetPins;
            BackgroundPatternBox.SelectedValue = _config.BackgroundPattern;
            BackgroundPrimaryColorBox.Text = LightCommand.NormalizeColor(_config.BackgroundPrimaryColor);
            BackgroundSecondaryColorBox.Text = LightCommand.NormalizeColor(_config.BackgroundSecondaryColor);
            BackgroundTertiaryColorBox.Text = LightCommand.NormalizeColor(_config.BackgroundTertiaryColor);
            BackgroundBrightnessSlider.Value = _config.BackgroundBrightness;
            BackgroundCycleSlider.Value = _config.BackgroundCycleMs;
            BackgroundStepSlider.Value = _config.BackgroundStepMs;
            RulesList.ItemsSource = _config.Rules;
            StripsList.ItemsSource = _config.LedStrips;
            SettingsPathText.Text = _settingsStore.SettingsPath;

            if (_config.Rules.Count > 0)
            {
                RulesList.SelectedIndex = 0;
            }

            if (_config.LedStrips.Count > 0)
            {
                StripsList.SelectedIndex = 0;
            }

            LoadSelectedRuleIntoUi();
            LoadSelectedStripIntoUi();
            ApplyTheme();
            UpdateStatusText();
        }
        finally
        {
            _loadingUi = false;
        }
    }

    private void LoadSelectedRuleIntoUi()
    {
        _loadingRule = true;

        try
        {
            RuleEditorPanel.IsEnabled = RulesList.SelectedItem is EventRule;

            if (RulesList.SelectedItem is not EventRule rule)
            {
                return;
            }

            RuleEnabledCheck.IsChecked = rule.IsEnabled;
            RuleNameBox.Text = rule.Name;
            EventKindBox.SelectedValue = rule.EventKind;
            RewardTitleBox.Text = rule.CustomRewardTitle;
            UseLightsCheck.IsChecked = rule.UseLights;
            PlayAudioCheck.IsChecked = rule.PlayAudio;
            AudioPathBox.Text = rule.AudioPath;
            PatternBox.SelectedValue = rule.Pattern;
            TargetPinsBox.Text = rule.TargetPins;
            PrimaryColorBox.Text = LightCommand.NormalizeColor(rule.PrimaryColor);
            SecondaryColorBox.Text = LightCommand.NormalizeColor(rule.SecondaryColor);
            TertiaryColorBox.Text = LightCommand.NormalizeColor(rule.TertiaryColor);
            BrightnessSlider.Value = rule.Brightness;
            DurationSlider.Value = rule.DurationMs;
            CycleSlider.Value = rule.CycleMs;
            StepSlider.Value = rule.StepMs;
            UpdateColorButtons();
            UpdateSliderLabels();
        }
        finally
        {
            _loadingRule = false;
        }
    }

    private void LoadSelectedStripIntoUi()
    {
        _loadingStrip = true;

        try
        {
            StripEditorPanel.IsEnabled = StripsList.SelectedItem is LedStripConfig;

            if (StripsList.SelectedItem is not LedStripConfig strip)
            {
                return;
            }

            StripNameBox.Text = strip.Name;
            StripPinBox.Text = strip.Pin.ToString();
            StripLedCountBox.Text = strip.LedCount.ToString();
        }
        finally
        {
            _loadingStrip = false;
        }
    }

    private void SaveGlobalSettingsFromFields()
    {
        if (_loadingUi)
        {
            return;
        }

        _config.TwitchClientId = ClientIdBox.Text.Trim();
        _config.SerialPort = ParsePort(PortComboBox.SelectedValue as string ?? PortComboBox.Text);
        _config.BaudRate = ParseInt(BaudRateBox.Text, 115200, 300, 921600);
        _config.AutoConnectTwitch = AutoTwitchCheck.IsChecked == true;
        _config.AutoConnectArduino = AutoArduinoCheck.IsChecked == true;
        _config.StartHidden = StartHiddenCheck.IsChecked == true;
        _config.DarkMode = DarkModeCheck.IsChecked == true;
    }

    private void SaveCurrentRuleFromFields()
    {
        if (_loadingRule || RulesList.SelectedItem is not EventRule rule)
        {
            return;
        }

        rule.IsEnabled = RuleEnabledCheck.IsChecked == true;
        rule.Name = RuleNameBox.Text.Trim();
        rule.EventKind = EventKindBox.SelectedValue is TwitchEventKind kind ? kind : TwitchEventKind.Follow;
        rule.CustomRewardTitle = RewardTitleBox.Text.Trim();
        rule.UseLights = UseLightsCheck.IsChecked == true;
        rule.PlayAudio = PlayAudioCheck.IsChecked == true;
        rule.AudioPath = AudioPathBox.Text.Trim();
        rule.Pattern = PatternBox.SelectedValue is LightPattern pattern ? pattern : LightPattern.Pulse;
        rule.TargetPins = string.Join(", ", LightCommand.ParsePins(TargetPinsBox.Text));
        rule.PrimaryColor = LightCommand.NormalizeColor(PrimaryColorBox.Text);
        rule.SecondaryColor = LightCommand.NormalizeColor(SecondaryColorBox.Text);
        rule.TertiaryColor = LightCommand.NormalizeColor(TertiaryColorBox.Text);
        rule.Brightness = (int)Math.Round(BrightnessSlider.Value);
        rule.DurationMs = (int)Math.Round(DurationSlider.Value);
        rule.CycleMs = (int)Math.Round(CycleSlider.Value);
        rule.StepMs = (int)Math.Round(StepSlider.Value);

        UpdateColorButtons();
        UpdateSliderLabels();
        RulesList.Items.Refresh();
    }

    private void SaveBackgroundFromFields()
    {
        _config.BackgroundEnabled = BackgroundEnabledCheck.IsChecked == true;
        _config.BackgroundTargetPins = string.Join(", ", LightCommand.ParsePins(BackgroundPinsBox.Text));
        _config.BackgroundPattern = BackgroundPatternBox.SelectedValue is LightPattern pattern ? pattern : LightPattern.Solid;
        _config.BackgroundPrimaryColor = LightCommand.NormalizeColor(BackgroundPrimaryColorBox.Text);
        _config.BackgroundSecondaryColor = LightCommand.NormalizeColor(BackgroundSecondaryColorBox.Text);
        _config.BackgroundTertiaryColor = LightCommand.NormalizeColor(BackgroundTertiaryColorBox.Text);
        _config.BackgroundBrightness = (int)Math.Round(BackgroundBrightnessSlider.Value);
        _config.BackgroundCycleMs = (int)Math.Round(BackgroundCycleSlider.Value);
        _config.BackgroundStepMs = (int)Math.Round(BackgroundStepSlider.Value);

        UpdateColorButtons();
        UpdateSliderLabels();
    }

    private void SaveCurrentStripFromFields()
    {
        if (_loadingStrip || StripsList.SelectedItem is not LedStripConfig strip)
        {
            return;
        }

        strip.Name = string.IsNullOrWhiteSpace(StripNameBox.Text)
            ? "Tira LED"
            : StripNameBox.Text.Trim();
        strip.Pin = ParseInt(StripPinBox.Text, 6, 0, 53);
        strip.LedCount = ParseInt(StripLedCountBox.Text, 30, 1, 600);

        StripsList.Items.Refresh();
        RulesList.Items.Refresh();
    }

    private void UpdateStatusText()
    {
        var channel = _config.Channel.IsReady ? _config.Channel.DisplayName : "sin login";
        TwitchStatusText.Text = _eventSubClient.IsRunning
            ? $"Escuchando eventos de {channel}."
            : $"Twitch: {channel}.";

        var totalLeds = _config.LedStrips.Sum(strip => strip.LedCount);
        ArduinoStatusText.Text = _lightController.HasOpenPort
            ? $"Arduino conectado en {_lightController.CurrentPort}. Tiras: {_config.LedStrips.Count}, LEDs: {totalLeds}."
            : $"Arduino sin conectar. Tiras: {_config.LedStrips.Count}, LEDs: {totalLeds}.";

        TwitchButton.Content = _eventSubClient.IsRunning ? "Desconectar Twitch" : "Conectar Twitch";
    }

    private void UpdateSliderLabels()
    {
        BrightnessValueText.Text = ((int)Math.Round(BrightnessSlider.Value)).ToString();
        DurationValueText.Text = $"{(int)Math.Round(DurationSlider.Value)} ms";
        CycleValueText.Text = $"{(int)Math.Round(CycleSlider.Value)} ms";
        StepValueText.Text = $"{(int)Math.Round(StepSlider.Value)} ms";
        BackgroundBrightnessValueText.Text = ((int)Math.Round(BackgroundBrightnessSlider.Value)).ToString();
        BackgroundCycleValueText.Text = $"{(int)Math.Round(BackgroundCycleSlider.Value)} ms";
        BackgroundStepValueText.Text = $"{(int)Math.Round(BackgroundStepSlider.Value)} ms";
    }

    private void UpdateColorButtons()
    {
        PrimaryColorButton.Background = ToBrush(PrimaryColorBox.Text);
        SecondaryColorButton.Background = ToBrush(SecondaryColorBox.Text);
        TertiaryColorButton.Background = ToBrush(TertiaryColorBox.Text);
        BackgroundPrimaryColorButton.Background = ToBrush(BackgroundPrimaryColorBox.Text);
        BackgroundSecondaryColorButton.Background = ToBrush(BackgroundSecondaryColorBox.Text);
        BackgroundTertiaryColorButton.Background = ToBrush(BackgroundTertiaryColorBox.Text);
    }

    private void ApplyTheme()
    {
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        Background = palette.Window;
        Resources["ThemeTextBrush"] = palette.Text;
        Resources["ThemeMutedTextBrush"] = palette.MutedText;
        Resources["ThemeInputBrush"] = palette.Input;
        Resources["ThemeBorderBrush"] = palette.Border;
        ApplyThemeToElement(this, palette);
        UpdateColorButtons();
    }

    private void ApplyThemeToElement(DependencyObject element, ThemePalette palette)
    {
        var skipChildren = false;

        switch (element)
        {
            case Border border:
                border.BorderBrush = palette.Border;
                border.Background = IsSidebarBorder(border)
                    ? palette.Sidebar
                    : palette.Surface;
                break;
            case TextBlock textBlock:
                textBlock.Foreground = textBlock.FontSize <= 12 || textBlock.Name.Contains("Status", StringComparison.OrdinalIgnoreCase)
                    ? palette.MutedText
                    : palette.Text;
                break;
            case System.Windows.Controls.TextBox textBox:
                textBox.Background = palette.Input;
                textBox.Foreground = palette.Text;
                textBox.BorderBrush = palette.Border;
                textBox.CaretBrush = palette.Text;
                break;
            case System.Windows.Controls.ComboBox comboBox:
                comboBox.Background = palette.Input;
                comboBox.Foreground = palette.Text;
                comboBox.BorderBrush = palette.Border;
                break;
            case System.Windows.Controls.ListBox listBox:
                listBox.Background = palette.Input;
                listBox.Foreground = palette.Text;
                listBox.BorderBrush = palette.Border;
                break;
            case System.Windows.Controls.TabControl tabControl:
                tabControl.Background = System.Windows.Media.Brushes.Transparent;
                tabControl.BorderBrush = palette.Border;
                tabControl.Foreground = palette.Text;
                break;
            case TabItem tabItem:
                tabItem.Background = palette.Surface;
                tabItem.Foreground = palette.Text;
                tabItem.BorderBrush = palette.Border;
                break;
            case System.Windows.Controls.CheckBox checkBox:
                checkBox.Foreground = palette.Text;
                checkBox.Background = palette.Input;
                checkBox.BorderBrush = palette.MutedText;
                skipChildren = true;
                break;
            case Slider slider:
                slider.Foreground = palette.Accent;
                break;
            case System.Windows.Controls.Button button when IsColorButton(button):
                button.BorderBrush = palette.Border;
                skipChildren = true;
                break;
            case System.Windows.Controls.Button button:
                ApplyButtonTheme(button, palette);
                skipChildren = true;
                break;
        }

        if (skipChildren)
        {
            return;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            ApplyThemeToElement(VisualTreeHelper.GetChild(element, i), palette);
        }
    }

    private void ApplyButtonTheme(System.Windows.Controls.Button button, ThemePalette palette)
    {
        if (ReferenceEquals(button.Style, Resources["PrimaryButton"]))
        {
            button.Background = palette.Accent;
            button.Foreground = System.Windows.Media.Brushes.White;
            button.BorderBrush = palette.Accent;
            return;
        }

        if (ReferenceEquals(button.Style, Resources["DangerButton"]))
        {
            button.Background = palette.DangerSurface;
            button.Foreground = palette.DangerText;
            button.BorderBrush = palette.DangerBorder;
            return;
        }

        button.Background = palette.Button;
        button.Foreground = palette.Text;
        button.BorderBrush = palette.Border;
    }

    private static bool IsColorButton(System.Windows.Controls.Button button)
    {
        return !string.IsNullOrWhiteSpace(button.Name)
            && button.Name.EndsWith("ColorButton", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSidebarBorder(Border border)
    {
        return VisualTreeHelper.GetParent(border) is Grid grid
            && grid.ColumnDefinitions.Count == 3
            && Grid.GetColumn(border) == 0;
    }

    private static SolidColorBrush ToBrush(string color)
    {
        try
        {
            return new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(LightCommand.NormalizeColor(color)));
        }
        catch
        {
            return new SolidColorBrush(Colors.White);
        }
    }

    private void PickColor(System.Windows.Controls.TextBox target)
    {
        using var dialog = new Forms.ColorDialog
        {
            FullOpen = true
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK)
        {
            return;
        }

        target.Text = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
    }

    private void CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Abrir", null, (_, _) => ShowFromTray());
        menu.Items.Add("Salir", null, async (_, _) => await ExitApplicationAsync());

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Luces Canje Twitch",
            Visible = true,
            ContextMenuStrip = menu
        };

        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private async Task ExitApplicationAsync()
    {
        _isExiting = true;
        SaveGlobalSettingsFromFields();
        SaveCurrentRuleFromFields();
        SaveCurrentStripFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        _backgroundApplyDebounce?.Cancel();
        _backgroundApplyDebounce?.Dispose();
        await _eventSubClient.StopAsync();
        _lightController.Dispose();
        _notifyIcon?.Dispose();
        Close();
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;
            SaveGlobalSettingsFromFields();
            SaveCurrentRuleFromFields();
            SaveCurrentStripFromFields();
            SaveBackgroundFromFields();
            SaveConfig();
            Hide();
            AddLog("Ventana oculta en segundo plano.");
            return;
        }

        await _eventSubClient.StopAsync();
        _lightController.Dispose();
        _notifyIcon?.Dispose();
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            AddLog("Ventana minimizada en segundo plano.");
        }
    }

    private void SaveConfig()
    {
        try
        {
            _settingsStore.Save(_config);
        }
        catch (Exception ex)
        {
            AddLog($"No pude guardar la configuracion: {ex.Message}");
        }
    }

    private void AddLog(string message)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _activity.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");

            while (_activity.Count > 250)
            {
                _activity.RemoveAt(_activity.Count - 1);
            }
        });
    }

    private static string ParsePort(string text)
    {
        var ports = text.Split([',', ';', ' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => value.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
            .Select(value => value.ToUpperInvariant())
            .ToArray();

        return ChoosePreferredPort(ports);
    }

    private static string ChoosePreferredPort(IReadOnlyList<string> ports)
    {
        if (ports.Count == 0)
        {
            return "";
        }

        return ports.FirstOrDefault(port => !string.Equals(port, "COM1", StringComparison.OrdinalIgnoreCase))
            ?? ports[0].ToUpperInvariant();
    }

    private static string ChoosePreferredPort(IReadOnlyList<SerialPortInfo> ports)
    {
        if (ports.Count == 0)
        {
            return "";
        }

        return ports.FirstOrDefault(port => port.IsLikelyArduino)?.PortName
            ?? ports.FirstOrDefault(port => !string.Equals(port.PortName, "COM1", StringComparison.OrdinalIgnoreCase))?.PortName
            ?? ports[0].PortName;
    }

    private static int ParseInt(string text, int fallback, int min, int max)
    {
        return int.TryParse(text, out var value)
            ? Math.Clamp(value, min, max)
            : fallback;
    }

    private sealed record UiOption<T>(string Label, T Value);

    private sealed record ThemePalette(
        SolidColorBrush Window,
        SolidColorBrush Sidebar,
        SolidColorBrush Surface,
        SolidColorBrush Input,
        SolidColorBrush Button,
        SolidColorBrush Border,
        SolidColorBrush Text,
        SolidColorBrush MutedText,
        SolidColorBrush Accent,
        SolidColorBrush DangerSurface,
        SolidColorBrush DangerText,
        SolidColorBrush DangerBorder)
    {
        public static ThemePalette Light { get; } = new(
            BrushFrom("#F7F4EF"),
            BrushFrom("#FFFCF7"),
            BrushFrom("#FFFFFF"),
            BrushFrom("#FFFFFF"),
            BrushFrom("#F8FAFC"),
            BrushFrom("#E4DED4"),
            BrushFrom("#1F2933"),
            BrushFrom("#667085"),
            BrushFrom("#216869"),
            BrushFrom("#FDF2F2"),
            BrushFrom("#B42318"),
            BrushFrom("#F4A7A0"));

        public static ThemePalette Dark { get; } = new(
            BrushFrom("#111318"),
            BrushFrom("#171A21"),
            BrushFrom("#1F2430"),
            BrushFrom("#151922"),
            BrushFrom("#242B38"),
            BrushFrom("#354052"),
            BrushFrom("#E7EAF0"),
            BrushFrom("#A6AFBF"),
            BrushFrom("#2EA3A5"),
            BrushFrom("#3A1F25"),
            BrushFrom("#FFB4A8"),
            BrushFrom("#7A3D45"));

        private static SolidColorBrush BrushFrom(string hex)
        {
            var brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }
    }
}
