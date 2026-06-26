using NeoTwitch.Services;
using NeoTwitch.Shared;
using NeoTwitch.Models;

namespace NeoTwitch;

public partial class MainWindow
{
    private void InitializeRuntimeUi()
    {
        _loadingUi = true;
        try
        {
            ActivityView.DataContext = _activityViewModel;
            ActivityList.ItemsSource = _activityViewModel.EntriesView;
            DashboardActivityList.ItemsSource = _activityViewModel.DashboardEntries;
            AudioLibraryList.ItemsSource = _audioLibraryRows;
            AudioGroupsList.ItemsSource = _audioGroupRows;
            ImageLibraryList.ItemsSource = _imageLibraryRows;
            ImageGroupsList.ItemsSource = _imageGroupRows;
            VideoLibraryList.ItemsSource = _videoLibraryRows;
            VideoGroupsList.ItemsSource = _videoGroupRows;
            ObsScenesList.ItemsSource = _obsSceneRows;

            InitializePreviewDots();
            InitializePreviewTimers();
            InitializeRulesBinding();
            InitializeRuleOptionSources();
            InitializeLibraryOptionSources();
            InitializeBackgroundOptionSources();
            InitializeConnectionOptionSources();

            VersionText.Text = $"V{NeoTwitchProduct.CurrentVersionText}";
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
