using System;
using System.Windows;
using System.Windows.Controls;

namespace NeoTwitch.Views;

public partial class AudioView : NeoTwitchView
{
    public AudioView()
    {
        InitializeComponent();
    }

    private void GlobalSettingsChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => Host?.GlobalSettingsChanged(sender, e);

    private void AudioLibraryGroupBox_DropDownClosed(object sender, EventArgs e) => Host?.AudioLibraryGroupBox_DropDownClosed(sender, e);
}
