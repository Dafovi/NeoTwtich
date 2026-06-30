using System.Windows;
using System.Windows.Controls;

namespace NeoTwitch.Views;

public partial class SettingsView : NeoTwitchView
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void GlobalSettingsChanged(object sender, RoutedEventArgs e) => Host?.GlobalSettingsChanged(sender, e);

    private void GlobalSettingsChanged(object sender, TextChangedEventArgs e) => Host?.GlobalSettingsChanged(sender, e);

    private void ThemeModeChanged(object sender, SelectionChangedEventArgs e) => Host?.ThemeModeChanged(sender, e);

}
