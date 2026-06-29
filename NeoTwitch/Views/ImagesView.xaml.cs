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

    private void ImageLibraryGroupBox_DropDownClosed(object sender, EventArgs e) => Host?.ImageLibraryGroupBox_DropDownClosed(sender, e);
}
