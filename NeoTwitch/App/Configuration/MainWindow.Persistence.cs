using System.Windows.Threading;
using NeoTwitch.Services.Text;

namespace NeoTwitch;

public partial class MainWindow
{
    private void SaveEditableStateFromFields()
    {
        SaveGlobalSettingsFromFields();
        SaveCurrentRuleFromFields();
        SaveCurrentStripFromFields();
        SaveBackgroundFromFields();
    }

    private void SaveConfig()
    {
        try
        {
            _settingsStore.Save(_config);
            if (!_initializingComponent)
            {
                if (Dispatcher.CheckAccess())
                {
                    UpdateDashboardSummary();
                }
                else
                {
                    _ = Dispatcher.BeginInvoke(UpdateDashboardSummary, DispatcherPriority.Background);
                }
            }
        }
        catch (Exception ex)
        {
            AddLog(_text.Format(UiTextKeys.SettingsSaveFailureLog, ex.Message));
        }
    }
}
