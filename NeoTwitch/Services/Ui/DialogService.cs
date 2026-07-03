using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfMessageBoxResult = System.Windows.MessageBoxResult;

namespace NeoTwitch.Services.Ui;

public enum DialogChoice
{
    Yes,
    No,
    Cancel
}

public sealed class DialogService
{
    public void ShowInformation(string title, string message)
    {
        Show(message, title, WpfMessageBoxButton.OK, WpfMessageBoxImage.Information);
    }

    public void ShowWarning(string title, string message)
    {
        Show(message, title, WpfMessageBoxButton.OK, WpfMessageBoxImage.Warning);
    }

    public bool Confirm(string title, string message)
    {
        return Show(message, title, WpfMessageBoxButton.YesNo, WpfMessageBoxImage.Question) == WpfMessageBoxResult.Yes;
    }

    public DialogChoice ConfirmWithCancel(string title, string message)
    {
        return Show(message, title, WpfMessageBoxButton.YesNoCancel, WpfMessageBoxImage.Warning) switch
        {
            WpfMessageBoxResult.Yes => DialogChoice.Yes,
            WpfMessageBoxResult.No => DialogChoice.No,
            _ => DialogChoice.Cancel
        };
    }

    private static WpfMessageBoxResult Show(
        string message,
        string title,
        WpfMessageBoxButton buttons,
        WpfMessageBoxImage image)
    {
        var owner = WpfApplication.Current?.MainWindow;
        return owner is null
            ? WpfMessageBox.Show(message, title, buttons, image)
            : WpfMessageBox.Show(owner, message, title, buttons, image);
    }
}
