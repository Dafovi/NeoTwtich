using System.Windows;
using System.Windows.Controls;

namespace NeoTwitch.Views;

public partial class ObsView : NeoTwitchView
{
    public ObsView()
    {
        InitializeComponent();
    }

    private void OpenObsGuideButton_Click(object sender, RoutedEventArgs e) => Host?.OpenObsGuideButton_Click(sender, e);

    private void TestObsButton_Click(object sender, RoutedEventArgs e) => Host?.TestObsButton_Click(sender, e);

    private void ConnectObsButton_Click(object sender, RoutedEventArgs e) => Host?.ConnectObsButton_Click(sender, e);

    private void ObsSceneChangeButton_Click(object sender, RoutedEventArgs e) => Host?.ObsSceneChangeButton_Click(sender, e);

    private void ObsScenePreviewButton_Click(object sender, RoutedEventArgs e) => Host?.ObsScenePreviewButton_Click(sender, e);

    private void ObsOverlaySettingsChanged(object sender, RoutedEventArgs e) => Host?.ObsOverlaySettingsChanged(sender, e);

    private void ObsOverlaySettingsChanged(object sender, TextChangedEventArgs e) => Host?.ObsOverlaySettingsChanged(sender, e);

    private void ObsOverlaySettingsChanged(object sender, SelectionChangedEventArgs e) => Host?.ObsOverlaySettingsChanged(sender, e);

    private void CopyObsOverlayUrlButton_Click(object sender, RoutedEventArgs e) => Host?.CopyObsOverlayUrlButton_Click(sender, e);
}
