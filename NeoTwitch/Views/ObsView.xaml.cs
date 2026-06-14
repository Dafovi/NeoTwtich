using System.Windows;

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
}
