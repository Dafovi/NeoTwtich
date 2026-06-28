namespace NeoTwitch.Services.Lights;

public static class SerialLightProtocol
{
    public const string FxCommand = "FX";
    public const string StopCommand = "STOP";
    public const string AckPrefix = "ACK|";
    public const string ErrorPrefix = "ERR|";

    public static string? ResolveCommandName(string line)
    {
        if (StartsWithCommand(line, FxCommand))
        {
            return FxCommand;
        }

        if (StartsWithCommand(line, StopCommand))
        {
            return StopCommand;
        }

        return null;
    }

    public static bool IsAckFor(string line, string commandName)
    {
        return string.Equals(line, $"{AckPrefix}{commandName}", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsError(string line)
    {
        return line.StartsWith(ErrorPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool StartsWithCommand(string line, string commandName)
    {
        return line.StartsWith($"{commandName}|", StringComparison.OrdinalIgnoreCase);
    }
}
