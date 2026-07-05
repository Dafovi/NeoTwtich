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
}
