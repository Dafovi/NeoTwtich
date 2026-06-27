using System.Text;

namespace NeoTwitch.Services.Diagnostics;

internal sealed class DiagnosticReportBuilder
{
    private readonly StringBuilder _body = new();

    public int WarningCount { get; private set; }

    public void Section(string title)
    {
        if (_body.Length > 0)
        {
            _body.AppendLine();
        }

        _body.AppendLine(title);
    }

    public void Ok(string message)
    {
        Line("[OK]", message);
    }

    public void Info(string message)
    {
        Line("[INFO]", message);
    }

    public void Warn(string message)
    {
        Line("[REVISAR]", message);
    }

    public string BuildBody()
    {
        return _body.ToString();
    }

    private void Line(string level, string message)
    {
        _body.AppendLine($"{level} {message}");
        if (string.Equals(level, "[REVISAR]", StringComparison.Ordinal))
        {
            WarningCount++;
        }
    }
}
