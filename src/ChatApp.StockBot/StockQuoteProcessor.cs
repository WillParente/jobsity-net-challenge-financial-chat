using ChatApp.StockBot.Stooq;

namespace ChatApp.StockBot;

/// <summary>
/// Turns a stock code into the chat reply the bot should post. Never throws:
/// every failure (unknown code, Stooq outage, malformed payload) becomes a
/// friendly message so the bot never goes silent and its consumer loop
/// never dies.
/// </summary>
public class StockQuoteProcessor
{
    private readonly IStooqClient _stooqClient;
    private readonly ILogger<StockQuoteProcessor> _logger;

    public StockQuoteProcessor(IStooqClient stooqClient, ILogger<StockQuoteProcessor> logger)
    {
        _stooqClient = stooqClient;
        _logger = logger;
    }

    public async Task<string> BuildReplyAsync(string stockCode, CancellationToken cancellationToken)
    {
        try
        {
            var csv = await _stooqClient.GetQuoteCsvAsync(stockCode, cancellationToken);
            var result = StooqCsvParser.Parse(csv);
            return QuoteMessageFormatter.Format(result, stockCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stooq request failed for stock code {StockCode}", stockCode);
            return QuoteMessageFormatter.ServiceUnavailable(stockCode);
        }
    }
}
