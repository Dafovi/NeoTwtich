using Forms = System.Windows.Forms;

namespace NeoTwitch.Services.Shell;

public static class TrayNotificationService
{
    public static bool TryShowBackgroundNotice(
        Forms.NotifyIcon notifyIcon,
        string title,
        string text,
        bool alreadyShown)
    {
        try
        {
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipText = text;
            notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Info;
            notifyIcon.ShowBalloonTip(alreadyShown ? 2500 : 4000);
            return true;
        }
        catch
        {
            // Windows can suppress tray notifications; the app still remains available in the tray.
            return false;
        }
    }
}
