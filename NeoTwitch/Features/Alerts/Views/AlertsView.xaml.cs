using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NeoTwitch.Services.Ui;

namespace NeoTwitch.Views;

public partial class AlertsView : NeoTwitchView
{
    private const double CategoryScrollStep = 260d;
    private const double CategoryScrollTolerance = 1d;

    public AlertsView()
    {
        InitializeComponent();
        AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler(RuleFilterButtonStateChanged));
        AddHandler(ToggleButton.UncheckedEvent, new RoutedEventHandler(RuleFilterButtonStateChanged));
    }

    private void RuleFieldChanged(object sender, RoutedEventArgs e) => Host?.RuleFieldChanged(sender, e);

    private void RuleFieldChanged(object sender, TextChangedEventArgs e) => Host?.RuleFieldChanged(sender, e);

    private void RuleFieldChanged(object sender, SelectionChangedEventArgs e) => Host?.RuleFieldChanged(sender, e);

    private void RuleFieldChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => Host?.RuleFieldChanged(sender, e);

    private void LightNumberBox_TextChanged(object sender, TextChangedEventArgs e) => Host?.LightNumberBox_TextChanged(sender, e);

    private void RuleLedPreviewPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) => Host?.RuleLedPreviewPanel_IsVisibleChanged(sender, e);

    private void AlertCategoryScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) => UpdateCategoryScrollButtons();

    private void AlertCategoryScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _ = Dispatcher.InvokeAsync(UpdateCategoryScrollButtons);
    }

    private void AlertCategoryScrollLeftButton_Click(object sender, RoutedEventArgs e)
    {
        AlertCategoryScrollViewer.ScrollToHorizontalOffset(Math.Max(0, AlertCategoryScrollViewer.HorizontalOffset - CategoryScrollStep));
        UpdateCategoryScrollButtons();
    }

    private void AlertCategoryScrollRightButton_Click(object sender, RoutedEventArgs e)
    {
        var maxOffset = Math.Max(0, AlertCategoryScrollViewer.ExtentWidth - AlertCategoryScrollViewer.ViewportWidth);
        AlertCategoryScrollViewer.ScrollToHorizontalOffset(Math.Min(maxOffset, AlertCategoryScrollViewer.HorizontalOffset + CategoryScrollStep));
        UpdateCategoryScrollButtons();
    }

    private void UpdateCategoryScrollButtons()
    {
        if (AlertCategoryScrollViewer is null
            || AlertCategoryScrollLeftButton is null
            || AlertCategoryScrollRightButton is null)
        {
            return;
        }

        var maxOffset = Math.Max(0, AlertCategoryScrollViewer.ExtentWidth - AlertCategoryScrollViewer.ViewportWidth);
        AlertCategoryScrollLeftButton.Visibility = AlertCategoryScrollViewer.HorizontalOffset > CategoryScrollTolerance
            ? Visibility.Visible
            : Visibility.Collapsed;
        AlertCategoryScrollRightButton.Visibility = AlertCategoryScrollViewer.HorizontalOffset < maxOffset - CategoryScrollTolerance
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void RuleFilterButtonStateChanged(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is ToggleButton button
            && IsRuleStatusFilterButton(button))
        {
            FilterButtonThemeService.Apply(
                button,
                button.IsChecked == true,
                "#14B8A6",
                CurrentPalette(),
                inactiveForeground: CurrentPalette().MutedText);
        }
    }

    private static bool IsRuleStatusFilterButton(ToggleButton button)
    {
        return button.Tag?.ToString()?.ToUpperInvariant() is "ALL" or "ACTIVE" or "INACTIVE";
    }

    private ThemePalette CurrentPalette()
    {
        return ReferenceEquals(TryFindResource("ThemeWindowBrush"), ThemePalette.Dark.Window)
            ? ThemePalette.Dark
            : ThemePalette.Light;
    }
}
