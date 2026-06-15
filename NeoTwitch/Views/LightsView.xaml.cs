using System.Windows;
using System.Windows.Controls;

namespace NeoTwitch.Views;

public partial class LightsView : NeoTwitchView
{
    public LightsView()
    {
        InitializeComponent();
    }

    private void AddStripButton_Click(object sender, RoutedEventArgs e) => Host?.AddStripButton_Click(sender, e);

    private void DuplicateStripButton_Click(object sender, RoutedEventArgs e) => Host?.DuplicateStripButton_Click(sender, e);

    private void RemoveStripButton_Click(object sender, RoutedEventArgs e) => Host?.RemoveStripButton_Click(sender, e);

    private void StripsList_SelectionChanged(object sender, SelectionChangedEventArgs e) => Host?.StripsList_SelectionChanged(sender, e);

    private void StripFieldChanged(object sender, TextChangedEventArgs e) => Host?.StripFieldChanged(sender, e);

    private void BackgroundFieldChanged(object sender, RoutedEventArgs e) => Host?.BackgroundFieldChanged(sender, e);

    private void BackgroundFieldChanged(object sender, TextChangedEventArgs e) => Host?.BackgroundFieldChanged(sender, e);

    private void BackgroundFieldChanged(object sender, SelectionChangedEventArgs e) => Host?.BackgroundFieldChanged(sender, e);

    private void BackgroundFieldChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => Host?.BackgroundFieldChanged(sender, e);

    private void BackgroundPatternTile_Click(object sender, RoutedEventArgs e) => Host?.BackgroundPatternTile_Click(sender, e);

    private void BackgroundLightValueButton_Click(object sender, RoutedEventArgs e) => Host?.BackgroundLightValueButton_Click(sender, e);

    private void BackgroundLightNumberBox_TextChanged(object sender, TextChangedEventArgs e) => Host?.BackgroundLightNumberBox_TextChanged(sender, e);

    private void BackgroundPrimaryColorButton_Click(object sender, RoutedEventArgs e) => Host?.BackgroundPrimaryColorButton_Click(sender, e);

    private void BackgroundSecondaryColorButton_Click(object sender, RoutedEventArgs e) => Host?.BackgroundSecondaryColorButton_Click(sender, e);

    private void BackgroundTertiaryColorButton_Click(object sender, RoutedEventArgs e) => Host?.BackgroundTertiaryColorButton_Click(sender, e);

    private void BackgroundLedPreviewPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) => Host?.BackgroundLedPreviewPanel_IsVisibleChanged(sender, e);

    private void ApplyArduinoBackgroundButton_Click(object sender, RoutedEventArgs e) => Host?.ApplyArduinoBackgroundButton_Click(sender, e);

    private void StopArduinoBackgroundButton_Click(object sender, RoutedEventArgs e) => Host?.StopArduinoBackgroundButton_Click(sender, e);

    private void OpenArduinoSketchButton_Click(object sender, RoutedEventArgs e) => Host?.OpenArduinoSketchButton_Click(sender, e);

    private void OpenArduinoGuideButton_Click(object sender, RoutedEventArgs e) => Host?.OpenArduinoGuideButton_Click(sender, e);
}
