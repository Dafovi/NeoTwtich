using System.Windows;
using NeoTwitch.Services.Text;

namespace NeoTwitch;

public partial class MainWindow
{
    internal async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveEditableStateFromFields();
        SaveConfig();
        await ApplyBackgroundStateAsync();
        AddLog(_text.Get(UiTextKeys.SettingsSavedLog));
    }
}
