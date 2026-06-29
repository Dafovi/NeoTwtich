using System.Windows;
using NeoTwitch.Services.Text;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSettingsFromUi();
    }

    private async void SaveSettingsFromUi()
    {
        SaveEditableStateFromFields();
        SaveConfig();
        await ApplyBackgroundStateAsync();
        AddLog(_text.Get(UiTextKeys.SettingsSavedLog));
    }
}
