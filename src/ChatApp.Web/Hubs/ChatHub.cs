using ChatApp.Contracts;
using ChatApp.Web.Messaging;
using ChatApp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public const string DefaultRoom = "general";
    private const int MaxMessageLength = 500;

    private readonly ChatMessageService _messages;
    private readonly IStockRequestPublisher _stockRequests;

    public ChatHub(ChatMessageService messages, IStockRequestPublisher stockRequests)
    {
        _messages = messages;
        _stockRequests = stockRequests;
    }

    private string CurrentUsername => Context.User?.Identity?.Name
        ?? throw new HubException("Unauthenticated connection.");

    /// <summary>
    /// Joins the caller to a room group and returns the room history
    /// (last 50 messages, oldest first). Also invoked on reconnect.
    /// </summary>
    public async Task<IReadOnlyList<ChatMessageDto>> JoinRoom(string roomName)
    {
        roomName = NormalizeRoom(roomName);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

        var history = await _messages.GetRecentMessagesAsync(roomName);
        return history.Select(ChatMessageDto.FromEntity).ToList();
    }

    public async Task SendMessage(string roomName, string text)
    {
        roomName = NormalizeRoom(roomName);
        text = (text ?? string.Empty).Trim();

        if (text.Length == 0)
        {
            return;
        }

        if (text.Length > MaxMessageLength)
        {
            text = text[..MaxMessageLength];
        }

        var parsed = StockCommandParser.Parse(text);
        switch (parsed.Outcome)
        {
            case CommandParseOutcome.StockCommand:
                await HandleStockCommandAsync(roomName, text, parsed.StockCode!);
                return;

            case CommandParseOutcome.InvalidStockCommand:
                await SendToCallerAsync("A stock code is required. Usage: /stock=aapl.us");
                return;

            case CommandParseOutcome.UnknownCommand:
                await SendToCallerAsync("Sorry, I did not understand that command. Try /stock=aapl.us");
                return;
        }

        var saved = await _messages.AddMessageAsync(roomName, CurrentUsername, text);
        await Clients.Group(roomName).SendAsync("ReceiveMessage", ChatMessageDto.FromEntity(saved));
    }

    /// <summary>
    /// Commands are never persisted (challenge requirement). The command is
    /// echoed live to the room so everyone can see what triggered the bot,
    /// and the request travels to the bot through RabbitMQ.
    /// </summary>
    private async Task HandleStockCommandAsync(string roomName, string commandText, string stockCode)
    {
        await Clients.Group(roomName).SendAsync(
            "ReceiveMessage", ChatMessageDto.EphemeralMessage(CurrentUsername, commandText));

        var request = new StockQuoteRequest(Guid.NewGuid(), stockCode, roomName, CurrentUsername);
        if (!_stockRequests.TryPublish(request))
        {
            await SendToCallerAsync("The stock service is currently unavailable. Please try again shortly.");
        }
    }

    private Task SendToCallerAsync(string content) =>
        Clients.Caller.SendAsync("ReceiveMessage", ChatMessageDto.EphemeralMessage("system", content));

    private static string NormalizeRoom(string roomName) =>
        string.IsNullOrWhiteSpace(roomName) ? DefaultRoom : roomName.Trim().ToLowerInvariant();
}
