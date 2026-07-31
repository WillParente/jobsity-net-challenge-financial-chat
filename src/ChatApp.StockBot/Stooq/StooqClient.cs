namespace ChatApp.StockBot.Stooq;

public class StooqClient : IStooqClient
{
    private readonly HttpClient _httpClient;

    public StooqClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetQuoteCsvAsync(string stockCode, CancellationToken cancellationToken)
    {
        var url = $"q/l/?s={Uri.EscapeDataString(stockCode)}&f=sd2t2ohlcv&h&e=csv";

        try
        {
            return await GetOnceAsync(url, cancellationToken);
        }
        catch (Exception ex) when (IsTransient(ex) && !cancellationToken.IsCancellationRequested)
        {
            // One retry: the free Stooq endpoint is occasionally slow.
            return await GetOnceAsync(url, cancellationToken);
        }
    }

    private async Task<string> GetOnceAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static bool IsTransient(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException;
}
