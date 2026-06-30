using System;

namespace NeoTwitch.Views;

public partial class VideosView : NeoTwitchView
{
    public VideosView()
    {
        InitializeComponent();
    }

    private void VideoLibraryGroupBox_DropDownClosed(object sender, EventArgs e) => Host?.VideoLibraryGroupBox_DropDownClosed(sender, e);
}
