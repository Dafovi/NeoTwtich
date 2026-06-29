using System;
using System.Windows;
using System.Windows.Controls;

namespace NeoTwitch.Views;

public partial class VideosView : NeoTwitchView
{
    public VideosView()
    {
        InitializeComponent();
    }

    private void VideoVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => Host?.VideoVolumeSlider_ValueChanged(sender, e);

    private void VideoLibraryGroupBox_DropDownClosed(object sender, EventArgs e) => Host?.VideoLibraryGroupBox_DropDownClosed(sender, e);
}
