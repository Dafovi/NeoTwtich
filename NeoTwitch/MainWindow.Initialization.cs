using NeoTwitch.Services;
using NeoTwitch.Shared;
using NeoTwitch.Models;
using NeoTwitch.ViewModels.Connections;
using NeoTwitch.ViewModels.Dashboard;
using NeoTwitch.ViewModels.Library;
using NeoTwitch.ViewModels.Obs;

namespace NeoTwitch;

public partial class MainWindow
{
    private void InitializeRuntimeUi()
    {
        _loadingUi = true;
        try
        {
            _dashboardViewModel = new DashboardViewModel(GoToActivity);
            _connectionsViewModel = new ConnectionsViewModel();
            _obsViewModel = new ObsViewModel();
            _audioLibraryViewModel = new LibraryScreenViewModel<AudioLibraryRow, AudioGroupRow>();
            _imageLibraryViewModel = new LibraryScreenViewModel<MediaLibraryRow, MediaGroupRow>();
            _videoLibraryViewModel = new LibraryScreenViewModel<MediaLibraryRow, MediaGroupRow>();
            DashboardView.DataContext = _dashboardViewModel;
            ConnectionsView.DataContext = _connectionsViewModel;
            ObsView.DataContext = _obsViewModel;
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
