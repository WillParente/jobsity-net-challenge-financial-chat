namespace ChatApp.Contracts;

/// <summary>
/// Published by the web app when a user sends /stock=stock_code.
/// Consumed by the stock bot.
/// </summary>
public sealed record StockQuoteRequest(
    Guid RequestId,
    string StockCode,
    string RoomName,
    string RequestedBy);
