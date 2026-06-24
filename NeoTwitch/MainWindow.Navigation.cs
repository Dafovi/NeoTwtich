using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace NeoTwitch;

public partial class MainWindow
{
    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, MainTabs) || _initializingComponent)
        {
            return;
        }

        UpdateNavigationButtons();
        if (int.TryParse(NavAudioButton.Tag?.ToString(), out var audioTabIndex)
            && MainTabs.SelectedIndex != audioTabIndex)
        {
            StopAudioPreview();
        }

        var isMediaPreviewTab =
            (int.TryParse(NavImagesButton.Tag?.ToString(), out var imagesTabIndex) && MainTabs.SelectedIndex == imagesTabIndex)
            || (int.TryParse(NavVideosButton.Tag?.ToString(), out var videosTabIndex) && MainTabs.SelectedIndex == videosTabIndex);
        if (!isMediaPreviewTab)
        {
            _ = StopMediaPreviewAsync();
        }

        UpdateRuleLedPreviewTimerState();
        UpdateBackgroundLedPreviewTimerState();
        ConfigureActionIcons();
        _ = Dispatcher.BeginInvoke(ApplyTheme, DispatcherPriority.Loaded);
        _ = Dispatcher.BeginInvoke(ApplyTheme, DispatcherPriority.ContextIdle);
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string tag }
            || !int.TryParse(tag, out var selectedIndex)
            || selectedIndex < 0
            || selectedIndex >= MainTabs.Items.Count)
        {
            return;
        }

        if (selectedIndex != MainTabs.SelectedIndex && !ResolvePendingRuleChanges())
        {
            return;
        }

        MainTabs.SelectedIndex = selectedIndex;
        UpdateNavigationButtons();
    }

    internal void GoToActivityButton_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(NavActivityButton.Tag?.ToString(), out var activityTabIndex))
        {
            if (activityTabIndex != MainTabs.SelectedIndex && !ResolvePendingRuleChanges())
            {
                return;
            }

            MainTabs.SelectedIndex = activityTabIndex;
        }

        UpdateNavigationButtons();
    }

    private async void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        await ExitApplicationAsync();
    }
}
