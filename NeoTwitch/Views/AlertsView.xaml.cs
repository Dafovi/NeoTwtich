using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NeoTwitch.Services.Ui;

namespace NeoTwitch.Views;

public partial class AlertsView : NeoTwitchView
{
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

    private void TargetPinsChoiceBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => Host?.TargetPinsChoiceBox_SelectionChanged(sender, e);

    private void LightNumberBox_TextChanged(object sender, TextChangedEventArgs e) => Host?.LightNumberBox_TextChanged(sender, e);

    private void RuleLedPreviewPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) => Host?.RuleLedPreviewPanel_IsVisibleChanged(sender, e);

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
