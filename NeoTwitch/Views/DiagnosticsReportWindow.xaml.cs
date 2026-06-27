using System.Windows;
using NeoTwitch.ViewModels.Status;
using WpfClipboard = System.Windows.Clipboard;

namespace NeoTwitch.Views;

public partial class DiagnosticsReportWindow : Window
{
    private readonly DiagnosticResult _result;
    private readonly Action? _reportCopied;

    public DiagnosticsReportWindow(DiagnosticResult result, Action? reportCopied = null)
    {
        InitializeComponent();
        _result = result;
        _reportCopied = reportCopied;

        TitleTextBlock.Text = result.WarningCount == 0
            ? "Diagnostico sin advertencias"
            : $"Diagnostico con {result.WarningCount} punto(s) por revisar";
        ReportTextBox.Text = result.Report;
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
