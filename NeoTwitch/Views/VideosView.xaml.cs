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

    private void BrowseNewVideoButton_Click(object sender, RoutedEventArgs e) => Host?.BrowseNewVideoButton_Click(sender, e);

    private void SaveNewVideoButton_Click(object sender, RoutedEventArgs e) => Host?.SaveNewVideoButton_Click(sender, e);

    private void AddVideoGroupButton_Click(object sender, RoutedEventArgs e) => Host?.AddVideoGroupButton_Click(sender, e);

    private void ViewVideoGroupButton_Click(object sender, RoutedEventArgs e) => Host?.ViewVideoGroupButton_Click(sender, e);

    private void DeleteVideoGroupButton_Click(object sender, RoutedEventArgs e) => Host?.DeleteVideoGroupButton_Click(sender, e);

    private void PreviewVideoButton_Click(object sender, RoutedEventArgs e) => Host?.PreviewVideoButton_Click(sender, e);

    private void DeleteVideoButton_Click(object sender, RoutedEventArgs e) => Host?.DeleteVideoButton_Click(sender, e);
}
