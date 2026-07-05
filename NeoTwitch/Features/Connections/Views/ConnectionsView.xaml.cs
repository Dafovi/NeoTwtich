using System;
using System.Windows;
using System.Windows.Controls;

namespace NeoTwitch.Views;

public partial class ConnectionsView : NeoTwitchView
{
    public ConnectionsView()
    {
        InitializeComponent();
    }

    private void GlobalSettingsChanged(object sender, RoutedEventArgs e) => Host?.GlobalSettingsChanged(sender, e);

    private void GlobalSettingsChanged(object sender, TextChangedEventArgs e) => Host?.GlobalSettingsChanged(sender, e);

    private void GlobalSettingsChanged(object sender, SelectionChangedEventArgs e) => Host?.GlobalSettingsChanged(sender, e);

    private void PortComboBox_DropDownOpened(object sender, EventArgs e) => Host?.PortComboBox_DropDownOpened(sender, e);

    private void AlexaSettingsChanged(object sender, RoutedEventArgs e) => Host?.AlexaSettingsChanged(sender, e);

    private void AlexaSettingsChanged(object sender, TextChangedEventArgs e) => Host?.AlexaSettingsChanged(sender, e);

    private void ObsSettingsChanged(object sender, RoutedEventArgs e) => Host?.ObsSettingsChanged(sender, e);

    private void ObsSettingsChanged(object sender, TextChangedEventArgs e) => Host?.ObsSettingsChanged(sender, e);

}
