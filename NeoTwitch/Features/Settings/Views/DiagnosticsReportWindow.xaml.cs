using System.Windows;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Status;
using WpfClipboard = System.Windows.Clipboard;

namespace NeoTwitch.Views;

public partial class DiagnosticsReportWindow : Window
{
    private readonly DiagnosticResult _result;
    private readonly Action? _reportCopied;

    public DiagnosticsReportWindow(DiagnosticResult result, IUiTextService text, Action? reportCopied = null)
    {
        InitializeComponent();
        _result = result;
        _reportCopied = reportCopied;

        Title = text.Get(UiTextKeys.DiagnosticsWindowTitle);
        TitleTextBlock.Text = result.WarningCount == 0
            ? text.Get(UiTextKeys.DiagnosticsNoWarningsTitle)
            : text.Format(UiTextKeys.DiagnosticsWarningsTitle, result.WarningCount);
        ReportTextBox.Text = result.Report;
        CopyButton.Content = text.Get(UiTextKeys.DiagnosticsCopyReport);
        CloseButton.Content = text.Get(UiTextKeys.DiagnosticsClose);
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        WpfClipboard.SetText(_result.Report);
        _reportCopied?.Invoke();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
