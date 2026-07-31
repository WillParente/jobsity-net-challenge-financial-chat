using System.Globalization;

namespace ChatApp.StockBot.Stooq;

/// <summary>
/// Parses the CSV returned by https://stooq.com/q/l/?s=...&amp;f=sd2t2ohlcv&amp;h&amp;e=csv:
///
///   Symbol,Date,Time,Open,High,Low,Close,Volume
///   AAPL.US,2026-07-30,22:00:07,208.9,213.58,208.1,213.42,49500000
///
/// Unknown stock codes come back with "N/D" in the value columns.
/// Never throws for bad input — every outcome is a <see cref="StooqQuoteResult"/>.
/// </summary>
public static class StooqCsvParser
{
    private const string NoData = "N/D";

    public static StooqQuoteResult Parse(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return StooqQuoteResult.Malformed();
        }

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
        {
            return StooqQuoteResult.Malformed();
        }

        // Columns are located by header name rather than fixed position so a
        // field-order change on Stooq's side does not silently break parsing.
        var header = lines[0].Split(',');
        var symbolIndex = Array.FindIndex(header, h => h.Equals("Symbol", StringComparison.OrdinalIgnoreCase));
        var closeIndex = Array.FindIndex(header, h => h.Equals("Close", StringComparison.OrdinalIgnoreCase));
        if (symbolIndex < 0 || closeIndex < 0)
        {
            return StooqQuoteResult.Malformed();
        }

        var row = lines[1].Split(',');
        if (row.Length <= Math.Max(symbolIndex, closeIndex))
        {
            return StooqQuoteResult.Malformed();
        }

        var symbol = row[symbolIndex];
        var close = row[closeIndex];

        if (close.Length == 0 || close.Equals(NoData, StringComparison.OrdinalIgnoreCase))
        {
            return StooqQuoteResult.NotFound(symbol);
        }

        // Invariant culture: "93.42" must parse to 93.42 regardless of the
        // host machine's decimal separator.
        if (!decimal.TryParse(close, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
        {
            return StooqQuoteResult.Malformed();
        }

        return StooqQuoteResult.Success(symbol, price);
    }
}
