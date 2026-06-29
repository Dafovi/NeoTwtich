using System.Windows;
using System.Windows.Controls;

namespace NeoTwitch.Views;

public partial class ObsView : NeoTwitchView
{
    public ObsView()
    {
        InitializeComponent();
    }

    private void ObsOverlaySettingsChanged(object sender, RoutedEventArgs e) => Host?.ObsOverlaySettingsChanged(sender, e);

    private void ObsOverlaySettingsChanged(object sender, TextChangedEventArgs e) => Host?.ObsOverlaySettingsChanged(sender, e);

    private void ObsOverlaySettingsChanged(object sender, SelectionChangedEventArgs e) => Host?.ObsOverlaySettingsChanged(sender, e);
}
