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

        _shellViewModel.SyncSelectedTab(MainTabs.SelectedIndex);
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

        _shellViewModel.NavigateTo(selectedIndex);
    }

    private void GoToActivity()
    {
        _shellViewModel.NavigateTo(ViewModels.Shell.ShellViewModel.ActivityTabIndex);
    }

    private bool TryNavigateToTab(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= MainTabs.Items.Count)
        {
            return false;
        }

        if (selectedIndex != MainTabs.SelectedIndex && !ResolvePendingRuleChanges())
        {
            return false;
        }

        MainTabs.SelectedIndex = selectedIndex;
        UpdateNavigationButtons();
        return true;
    }

    private async void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        await ExitApplicationAsync();
    }
}
