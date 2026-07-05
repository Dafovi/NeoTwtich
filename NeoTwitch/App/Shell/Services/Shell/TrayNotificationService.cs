using Forms = System.Windows.Forms;

namespace NeoTwitch.Services.Shell;

public static class TrayNotificationService
{
    public static bool TryShowNotice(
        Forms.NotifyIcon notifyIcon,
        string title,
        string text,
        Forms.ToolTipIcon icon = Forms.ToolTipIcon.Info,
        int timeoutMs = 4000)
    {
        try
        {
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipText = text;
            notifyIcon.BalloonTipIcon = icon;
            notifyIcon.ShowBalloonTip(timeoutMs);
            return true;
        }
        catch
        {
            // Windows can suppress tray notifications; the app still remains available in the tray.
            return false;
        }
    }

    public static bool TryShowBackgroundNotice(
        Forms.NotifyIcon notifyIcon,
        string title,
        string text,
        bool alreadyShown)
    {
        return TryShowNotice(notifyIcon, title, text, timeoutMs: alreadyShown ? 2500 : 4000);
    }
}
