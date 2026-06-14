using System.Windows.Controls.Primitives;

namespace NeoTwitch;

public partial class MainWindow
{
    private System.Windows.Controls.TextBox ActivitySearchBox => ActivityView.ActivitySearchBox;
    private System.Windows.Controls.ListBox ActivityList => ActivityView.ActivityList;
    private ToggleButton ActivityFilterTwitchButton => ActivityView.ActivityFilterTwitchButton;
    private ToggleButton ActivityFilterArduinoButton => ActivityView.ActivityFilterArduinoButton;
    private ToggleButton ActivityFilterAlexaButton => ActivityView.ActivityFilterAlexaButton;
    private ToggleButton ActivityFilterAudioButton => ActivityView.ActivityFilterAudioButton;
    private ToggleButton ActivityFilterObsButton => ActivityView.ActivityFilterObsButton;
    private ToggleButton ActivityFilterEventButton => ActivityView.ActivityFilterEventButton;
    private ToggleButton ActivityFilterSystemButton => ActivityView.ActivityFilterSystemButton;
    private ToggleButton ActivityFilterImportantButton => ActivityView.ActivityFilterImportantButton;
}
