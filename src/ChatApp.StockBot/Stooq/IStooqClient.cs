namespace ChatApp.StockBot.Stooq;

public interface IStooqClient
{
    /// <summary>Fetches the raw quote CSV for a stock code (e.g. "aapl.us").</summary>
    Task<string> GetQuoteCsvAsync(string stockCode, CancellationToken cancellationToken);
}
