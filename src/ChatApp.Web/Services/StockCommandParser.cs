using System.Text.RegularExpressions;

namespace ChatApp.Web.Services;

public enum CommandParseOutcome
{
    /// <summary>Plain chat text — persist and broadcast normally.</summary>
    NotACommand,

    /// <summary>A well-formed /stock=stock_code command.</summary>
    StockCommand,

    /// <summary>A /stock command with a missing or invalid stock code.</summary>
    InvalidStockCommand,

    /// <summary>Any other "/" prefixed message (bonus: "messages that are not understood").</summary>
    UnknownCommand,
}

public sealed record CommandParseResult(CommandParseOutcome Outcome, string? StockCode)
{
    public static readonly CommandParseResult NotACommand = new(CommandParseOutcome.NotACommand, null);
    public static readonly CommandParseResult Invalid = new(CommandParseOutcome.InvalidStockCommand, null);
    public static readonly CommandParseResult Unknown = new(CommandParseOutcome.UnknownCommand, null);
}

/// <summary>
/// Classifies chat input. Only messages *starting* with "/" are command
/// attempts, and no command attempt is ever persisted as a chat message.
/// </summary>
public static partial class StockCommandParser
{
    [GeneratedRegex(@"^/stock=(?<code>.*)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StockCommandRegex();

    public static CommandParseResult Parse(string? text)
    {
        text = text?.Trim() ?? string.Empty;

        if (!text.StartsWith('/'))
        {
            return CommandParseResult.NotACommand;
        }

        var match = StockCommandRegex().Match(text);
        if (!match.Success)
        {
            return CommandParseResult.Unknown;
        }

        var code = match.Groups["code"].Value.Trim();
        if (code.Length == 0 || code.Any(char.IsWhiteSpace))
        {
            return CommandParseResult.Invalid;
        }

        // Normalized to lower case: Stooq expects e.g. "aapl.us".
        return new CommandParseResult(CommandParseOutcome.StockCommand, code.ToLowerInvariant());
    }
}
