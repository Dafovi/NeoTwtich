using System.Windows;
using System.Windows.Controls;

namespace NeoTwitch.Views;

public partial class AlexaView : NeoTwitchView
{
    public AlexaView()
    {
        InitializeComponent();
    }

    private void BackgroundFieldChanged(object sender, RoutedEventArgs e) => Host?.BackgroundFieldChanged(sender, e);

    private void BackgroundFieldChanged(object sender, TextChangedEventArgs e) => Host?.BackgroundFieldChanged(sender, e);

    private void ApplyAlexaBackgroundButton_Click(object sender, RoutedEventArgs e) => Host?.ApplyAlexaBackgroundButton_Click(sender, e);

    private void StopAlexaBackgroundButton_Click(object sender, RoutedEventArgs e) => Host?.StopAlexaBackgroundButton_Click(sender, e);
}
