using NeoTwitch.Services.Text;

namespace NeoTwitch;

public partial class MainWindow
{
    private async void SaveSettingsFromUi()
    {
        SaveEditableStateFromFields();
        SaveConfig();
        await ApplyBackgroundStateAsync();
        AddLog(_text.Get(UiTextKeys.SettingsSavedLog));
    }
}
