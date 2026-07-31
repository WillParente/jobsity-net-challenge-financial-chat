namespace ChatApp.Contracts;

/// <summary>
/// Published by the stock bot with the chat message to post in the room.
/// Consumed by the web app, which persists and broadcasts it as the bot.
/// </summary>
public sealed record StockQuoteReply(
    Guid RequestId,
    string RoomName,
    string Message);
