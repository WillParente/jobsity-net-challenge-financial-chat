namespace ChatApp.StockBot.Stooq;

public enum StooqQuoteStatus
{
    /// <summary>The CSV contained a valid closing price.</summary>
    Success,

    /// <summary>Stooq answered with "N/D" fields — the stock code is unknown.</summary>
    NotFound,

    /// <summary>The response could not be parsed as a Stooq quote CSV.</summary>
    Malformed,
}

public sealed record StooqQuoteResult(StooqQuoteStatus Status, string? Symbol, decimal Price)
{
    public static StooqQuoteResult Success(string symbol, decimal price) =>
        new(StooqQuoteStatus.Success, symbol, price);

    public static StooqQuoteResult NotFound(string? symbol) =>
        new(StooqQuoteStatus.NotFound, symbol, 0m);

    public static StooqQuoteResult Malformed() =>
        new(StooqQuoteStatus.Malformed, null, 0m);
}
