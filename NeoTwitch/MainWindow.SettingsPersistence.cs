using System.Windows;

namespace NeoTwitch;

public partial class MainWindow
{
    internal async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveEditableStateFromFields();
        SaveConfig();
        await ApplyBackgroundStateAsync();
        AddLog("Configuracion guardada.");
    }
}
