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

    private void SaveButton_Click(object sender, RoutedEventArgs e) => Host?.SaveButton_Click(sender, e);

    private void GlobalSettingsChanged(object sender, RoutedEventArgs e) => Host?.GlobalSettingsChanged(sender, e);

    private void GlobalSettingsChanged(object sender, TextChangedEventArgs e) => Host?.GlobalSettingsChanged(sender, e);

    private void GlobalSettingsChanged(object sender, SelectionChangedEventArgs e) => Host?.GlobalSettingsChanged(sender, e);

    private void TwitchButton_Click(object sender, RoutedEventArgs e) => Host?.TwitchButton_Click(sender, e);

    private void OpenTwitchConsoleButton_Click(object sender, RoutedEventArgs e) => Host?.OpenTwitchConsoleButton_Click(sender, e);

    private void ToggleClientIdVisibility_Click(object sender, RoutedEventArgs e) => Host?.ToggleClientIdVisibility_Click(sender, e);

    private void ToggleClientSecretVisibility_Click(object sender, RoutedEventArgs e) => Host?.ToggleClientSecretVisibility_Click(sender, e);

    private void ConnectArduinoButton_Click(object sender, RoutedEventArgs e) => Host?.ConnectArduinoButton_Click(sender, e);

    private void DetectPortsButton_Click(object sender, RoutedEventArgs e) => Host?.DetectPortsButton_Click(sender, e);

    private void PortComboBox_DropDownOpened(object sender, EventArgs e) => Host?.PortComboBox_DropDownOpened(sender, e);

    private void AlexaSettingsChanged(object sender, RoutedEventArgs e) => Host?.AlexaSettingsChanged(sender, e);

    private void AlexaSettingsChanged(object sender, TextChangedEventArgs e) => Host?.AlexaSettingsChanged(sender, e);

    private void OpenAlexaConsoleButton_Click(object sender, RoutedEventArgs e) => Host?.OpenAlexaConsoleButton_Click(sender, e);

    private void TestAlexaButton_Click(object sender, RoutedEventArgs e) => Host?.TestAlexaButton_Click(sender, e);

    private void ToggleAlexaRelayUrlVisibility_Click(object sender, RoutedEventArgs e) => Host?.ToggleAlexaRelayUrlVisibility_Click(sender, e);

    private void ToggleAlexaAuthTokenVisibility_Click(object sender, RoutedEventArgs e) => Host?.ToggleAlexaAuthTokenVisibility_Click(sender, e);

    private void ObsSettingsChanged(object sender, RoutedEventArgs e) => Host?.ObsSettingsChanged(sender, e);

    private void ObsSettingsChanged(object sender, TextChangedEventArgs e) => Host?.ObsSettingsChanged(sender, e);

    private void OpenObsGuideButton_Click(object sender, RoutedEventArgs e) => Host?.OpenObsGuideButton_Click(sender, e);

    private void ConnectObsButton_Click(object sender, RoutedEventArgs e) => Host?.ConnectObsButton_Click(sender, e);

    private void TestObsButton_Click(object sender, RoutedEventArgs e) => Host?.TestObsButton_Click(sender, e);

    private void ToggleObsPasswordVisibility_Click(object sender, RoutedEventArgs e) => Host?.ToggleObsPasswordVisibility_Click(sender, e);
}
