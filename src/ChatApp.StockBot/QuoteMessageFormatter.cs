using System.Globalization;
using ChatApp.StockBot.Stooq;

namespace ChatApp.StockBot;

/// <summary>
/// Builds the chat messages posted by the bot. The success format is fixed
/// by the challenge: "AAPL.US quote is $93.42 per share".
/// </summary>
public static class QuoteMessageFormatter
{
    public static string Format(StooqQuoteResult result, string requestedCode) => result.Status switch
    {
        StooqQuoteStatus.Success => string.Create(
            CultureInfo.InvariantCulture,
            $"{result.Symbol!.ToUpperInvariant()} quote is ${result.Price:0.00} per share"),

        StooqQuoteStatus.NotFound =>
            $"No quote is available for \"{Display(requestedCode)}\". Please check the stock code (e.g. /stock=aapl.us).",

        _ => $"Could not read quote data for \"{Display(requestedCode)}\". Please try again later.",
    };

    public static string ServiceUnavailable(string requestedCode) =>
        $"The stock service is not responding for \"{Display(requestedCode)}\". Please try again shortly.";

    private static string Display(string requestedCode) => requestedCode.Trim().ToUpperInvariant();
}
