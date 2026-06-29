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

    private void BrowseNewAudioButton_Click(object sender, RoutedEventArgs e) => Host?.BrowseNewAudioButton_Click(sender, e);

    private void SaveNewAudioButton_Click(object sender, RoutedEventArgs e) => Host?.SaveNewAudioButton_Click(sender, e);

    private void AddAudioGroupButton_Click(object sender, RoutedEventArgs e) => Host?.AddAudioGroupButton_Click(sender, e);

    private void ViewAudioGroupButton_Click(object sender, RoutedEventArgs e) => Host?.ViewAudioGroupButton_Click(sender, e);

    private void DeleteAudioGroupButton_Click(object sender, RoutedEventArgs e) => Host?.DeleteAudioGroupButton_Click(sender, e);

    private void PreviewAudioButton_Click(object sender, RoutedEventArgs e) => Host?.PreviewAudioButton_Click(sender, e);

    private void DeleteAudioButton_Click(object sender, RoutedEventArgs e) => Host?.DeleteAudioButton_Click(sender, e);
}
