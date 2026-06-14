using System;
using System.Windows;
using System.Windows.Controls;

namespace NeoTwitch.Views;

public partial class ImagesView : NeoTwitchView
{
    public ImagesView()
    {
        InitializeComponent();
    }

    private void ImageSearchBox_TextChanged(object sender, TextChangedEventArgs e) => Host?.ImageSearchBox_TextChanged(sender, e);

    private void ImageFilterButton_Click(object sender, RoutedEventArgs e) => Host?.ImageFilterButton_Click(sender, e);

    private void ImageLibraryGroupBox_DropDownClosed(object sender, EventArgs e) => Host?.ImageLibraryGroupBox_DropDownClosed(sender, e);

    private void BrowseNewImageButton_Click(object sender, RoutedEventArgs e) => Host?.BrowseNewImageButton_Click(sender, e);

    private void SaveNewImageButton_Click(object sender, RoutedEventArgs e) => Host?.SaveNewImageButton_Click(sender, e);

    private void AddImageGroupButton_Click(object sender, RoutedEventArgs e) => Host?.AddImageGroupButton_Click(sender, e);

    private void ViewImageGroupButton_Click(object sender, RoutedEventArgs e) => Host?.ViewImageGroupButton_Click(sender, e);

    private void DeleteImageGroupButton_Click(object sender, RoutedEventArgs e) => Host?.DeleteImageGroupButton_Click(sender, e);

    private void DeleteImageButton_Click(object sender, RoutedEventArgs e) => Host?.DeleteImageButton_Click(sender, e);
}
