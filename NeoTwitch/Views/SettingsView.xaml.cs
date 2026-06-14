using System.Windows;
using System.Windows.Controls;

namespace NeoTwitch.Views;

public partial class SettingsView : NeoTwitchView
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void GlobalSettingsChanged(object sender, RoutedEventArgs e) => Host?.GlobalSettingsChanged(sender, e);

    private void GlobalSettingsChanged(object sender, TextChangedEventArgs e) => Host?.GlobalSettingsChanged(sender, e);

    private void ThemeModeChanged(object sender, SelectionChangedEventArgs e) => Host?.ThemeModeChanged(sender, e);

    private void ImportSettingsButton_Click(object sender, RoutedEventArgs e) => Host?.ImportSettingsButton_Click(sender, e);

    private void ExportSettingsButton_Click(object sender, RoutedEventArgs e) => Host?.ExportSettingsButton_Click(sender, e);

    private void CreateBackupButton_Click(object sender, RoutedEventArgs e) => Host?.CreateBackupButton_Click(sender, e);

    private void RestoreBackupButton_Click(object sender, RoutedEventArgs e) => Host?.RestoreBackupButton_Click(sender, e);

    private void RunDiagnosticsButton_Click(object sender, RoutedEventArgs e) => Host?.RunDiagnosticsButton_Click(sender, e);

    private void CloseBehaviorRadio_Checked(object sender, RoutedEventArgs e) => Host?.CloseBehaviorRadio_Checked(sender, e);

    private void SaveButton_Click(object sender, RoutedEventArgs e) => Host?.SaveButton_Click(sender, e);
}
