using NeoTwitch.Services;
using NeoTwitch.Shared;
using NeoTwitch.Models;
using NeoTwitch.ViewModels.Alexa;
using NeoTwitch.ViewModels.Alerts;
using NeoTwitch.ViewModels.Connections;
using NeoTwitch.ViewModels.Dashboard;
using NeoTwitch.ViewModels.Library;
using NeoTwitch.ViewModels.Lights;
using NeoTwitch.ViewModels.Obs;
using NeoTwitch.ViewModels.Settings;

namespace NeoTwitch;

public partial class MainWindow
{
    private void InitializeRuntimeUi()
    {
        _loadingUi = true;
        try
        {
            _dashboardViewModel = new DashboardViewModel(GoToActivity);
            _alertsViewModel = new AlertsViewModel(_ruleCategoryOptions, _text);
            _alertsViewModel.ConfigureActions(
                AddRule,
                DuplicateSelectedRule,
                ToggleRuleTest,
                SavePendingRuleChanges,
                RemoveSelectedRule);
            _alertsViewModel.ConfigureEditorActions(
                SelectRuleEventKind,
                SelectRuleLightPattern,
                SelectRuleLightPreset,
                AdjustRuleLightValue,
                SelectRuleAudioMode,
                SelectRuleObsMediaKind,
                SelectRuleObsMediaSourceMode);
            _alertsViewModel.FiltersChanged += AlertsFiltersChanged;
            _alertsViewModel.SelectedRuleChanged += AlertsSelectedRuleChanged;
            _connectionsViewModel = new ConnectionsViewModel();
            _alexaViewModel = new AlexaViewModel();
            _alexaViewModel.ConfigureActions(ApplyAlexaBackground, StopAlexaBackground);
            _connectionsViewModel.ConfigureActions(
                SaveSettingsFromUi,
                ToggleTwitchConnection,
                OpenTwitchConsole,
                ToggleClientIdVisibility,
                ToggleClientSecretVisibility,
                DetectPorts,
                ConnectArduino,
                OpenAlexaConsole,
                TestAlexaConnection,
                ToggleAlexaRelayUrlVisibility,
                ToggleAlexaAuthTokenVisibility,
                OpenObsGuide,
                ToggleObsConnection,
                TestObsConnection,
                ToggleObsPasswordVisibility);
            _obsViewModel = new ObsViewModel();
            _obsViewModel.ConfigureActions(
                CopyObsOverlayUrl,
                TestObsConnection,
                PreviewObsScene,
                ChangeObsScene);
            _lightsViewModel = new LightsViewModel();
            _lightsViewModel.ConfigureActions(
                AddStrip,
                DuplicateStrip,
                RemoveStrip,
                ApplyArduinoBackground,
                StopArduinoBackground,
                OpenArduinoSketch,
                OpenArduinoGuide);
            _lightsViewModel.ConfigureEditorActions(
                SelectBackgroundPattern,
                AdjustBackgroundLightValue,
                SelectBackgroundLightPreset);
            _settingsViewModel = new SettingsViewModel();
            _audioLibraryViewModel = new LibraryScreenViewModel<AudioLibraryRow, AudioGroupRow>();
            _imageLibraryViewModel = new LibraryScreenViewModel<MediaLibraryRow, MediaGroupRow>();
            _videoLibraryViewModel = new LibraryScreenViewModel<MediaLibraryRow, MediaGroupRow>();
            _settingsViewModel.ConfigureActions(
                ImportSettings,
                ExportSettings,
                CreateBackup,
                RestoreBackup,
                RunDiagnostics,
                SaveSettingsFromUi);
            _audioLibraryViewModel.ConfigureActions(BrowseNewAudio, SaveNewAudio, AddAudioGroup, ViewAudioGroup, DeleteAudioGroup, PreviewAudio, DeleteAudio);
            _imageLibraryViewModel.ConfigureActions(
                () => BrowseNewMedia(MediaLibraryKind.Image),
                () => SaveNewMedia(MediaLibraryKind.Image),
                () => AddMediaGroup(MediaLibraryKind.Image),
                parameter => ViewMediaGroup(MediaLibraryKind.Image, parameter),
                parameter => DeleteMediaGroup(MediaLibraryKind.Image, parameter),
                parameter => PreviewMediaAsset(MediaLibraryKind.Image, parameter),
                parameter => DeleteMediaAsset(MediaLibraryKind.Image, parameter));
            _videoLibraryViewModel.ConfigureActions(
                () => BrowseNewMedia(MediaLibraryKind.Video),
                () => SaveNewMedia(MediaLibraryKind.Video),
                () => AddMediaGroup(MediaLibraryKind.Video),
                parameter => ViewMediaGroup(MediaLibraryKind.Video, parameter),
                parameter => DeleteMediaGroup(MediaLibraryKind.Video, parameter),
                parameter => PreviewMediaAsset(MediaLibraryKind.Video, parameter),
                parameter => DeleteMediaAsset(MediaLibraryKind.Video, parameter));
            _audioLibraryViewModel.FiltersChanged += AudioLibraryFiltersChanged;
            _imageLibraryViewModel.FiltersChanged += ImageLibraryFiltersChanged;
            _videoLibraryViewModel.FiltersChanged += VideoLibraryFiltersChanged;
            DashboardView.DataContext = _dashboardViewModel;
            AlertsView.DataContext = _alertsViewModel;
            AlexaView.DataContext = _alexaViewModel;
            ConnectionsView.DataContext = _connectionsViewModel;
            LightsView.DataContext = _lightsViewModel;
            ObsView.DataContext = _obsViewModel;
            SettingsView.DataContext = _settingsViewModel;
            ActivityView.DataContext = _activityViewModel;
            AudioView.DataContext = _audioLibraryViewModel;
            ImagesView.DataContext = _imageLibraryViewModel;
            VideosView.DataContext = _videoLibraryViewModel;
            ActivityList.ItemsSource = _activityViewModel.EntriesView;
            DashboardActivityList.ItemsSource = _activityViewModel.DashboardEntries;

            InitializePreviewDots();
            InitializePreviewTimers();
            InitializeRulesBinding();
            InitializeRuleOptionSources();
            InitializeLibraryOptionSources();
            InitializeBackgroundOptionSources();
            InitializeConnectionOptionSources();

            VersionText.Text = _shellViewModel.VersionText;
            ConfigureNavigationIcons();
            ConfigureActionIcons();
            ArrangeAlertActionCards();
            RefreshPortList(choosePreferred: false);
        }
        finally
        {
            _loadingUi = false;
        }
    }

    private void InitializePreviewDots()
    {
        for (var i = 0; i < ApplicationLimits.RulePreviewLedDots; i++)
        {
            _ruleLedPreviewDots.Add(PreviewDot(Services.Lights.LedPreviewService.ParseColor("#334155", "#334155"), 0.08));
            _backgroundLedPreviewDots.Add(PreviewDot(Services.Lights.LedPreviewService.ParseColor("#334155", "#334155"), 0.08));
        }

        RuleLedPreviewList.ItemsSource = _ruleLedPreviewDots;
        BackgroundLedPreviewList.ItemsSource = _backgroundLedPreviewDots;
    }

    private void InitializePreviewTimers()
    {
        _ruleLedPreviewTimer.Interval = TimeSpan.FromMilliseconds(120);
        _ruleLedPreviewTimer.Tick += (_, _) => UpdateRuleLedPreviewFrame();
        _backgroundLedPreviewTimer.Interval = TimeSpan.FromMilliseconds(120);
        _backgroundLedPreviewTimer.Tick += (_, _) => UpdateBackgroundLedPreviewFrame();
        _arduinoMonitorTimer.Interval = TimeSpan.FromSeconds(2.5);
        _arduinoMonitorTimer.Tick += ArduinoMonitorTimer_Tick;
    }

}
