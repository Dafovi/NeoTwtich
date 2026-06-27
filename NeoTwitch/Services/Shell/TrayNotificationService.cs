using Forms = System.Windows.Forms;

namespace NeoTwitch.Services.Shell;

public static class TrayNotificationService
{
    public static bool TryShowBackgroundNotice(Forms.NotifyIcon notifyIcon, string appName, bool alreadyShown)
    {
        try
        {
            notifyIcon.BalloonTipTitle = $"{appName} sigue activo";
            notifyIcon.BalloonTipText = "La app quedo en segundo plano. Abrela desde el icono de la bandeja cuando la necesites.";
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
