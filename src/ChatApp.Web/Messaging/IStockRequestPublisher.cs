using ChatApp.Contracts;

namespace ChatApp.Web.Messaging;

public interface IStockRequestPublisher
{
    /// <summary>
    /// Publishes a stock quote request to the broker. Returns false instead
    /// of throwing when the broker is unreachable so the chat keeps working
    /// and the caller can inform the user.
    /// </summary>
    bool TryPublish(StockQuoteRequest request);
}
