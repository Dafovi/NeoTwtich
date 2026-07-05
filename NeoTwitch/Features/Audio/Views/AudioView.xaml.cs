using System;

namespace NeoTwitch.Views;

public partial class AudioView : NeoTwitchView
{
    public AudioView()
    {
        InitializeComponent();
    }

    private void AudioLibraryGroupBox_DropDownClosed(object sender, EventArgs e) => Host?.AudioLibraryGroupBox_DropDownClosed(sender, e);
}
