using System.Text;

namespace NeoTwitch.Services.Diagnostics;

internal sealed class DiagnosticReportBuilder
{
    private readonly StringBuilder _body = new();
    private readonly string _okLevel;
    private readonly string _infoLevel;
    private readonly string _warningLevel;

    public DiagnosticReportBuilder(string okLevel, string infoLevel, string warningLevel)
    {
        _okLevel = okLevel;
        _infoLevel = infoLevel;
        _warningLevel = warningLevel;
    }

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
        Line(_okLevel, message);
    }

    public void Info(string message)
    {
        Line(_infoLevel, message);
    }

    public void Warn(string message)
    {
        Line(_warningLevel, message);
    }

    public string BuildBody()
    {
        return _body.ToString();
    }

    private void Line(string level, string message)
    {
        _body.AppendLine($"{level} {message}");
        if (string.Equals(level, _warningLevel, StringComparison.Ordinal))
        {
            WarningCount++;
        }
    }
}
