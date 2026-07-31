using ChatApp.Contracts;
using ChatApp.Web.Messaging;
using ChatApp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private const int MaxMessageLength = 500;

    private readonly ChatMessageService _messages;
    private readonly RoomService _rooms;
    private readonly IStockRequestPublisher _stockRequests;

    public ChatHub(ChatMessageService messages, RoomService rooms, IStockRequestPublisher stockRequests)
    {
        _messages = messages;
        _rooms = rooms;
        _stockRequests = stockRequests;
    }

    private string CurrentUsername => Context.User?.Identity?.Name
        ?? throw new HubException("Unauthenticated connection.");

    /// <summary>Room names available to the room selector (bonus: multiple chatrooms).</summary>
    public Task<IReadOnlyList<string>> GetRooms() => _rooms.GetRoomNamesAsync();

    /// <summary>
    /// Joins the caller to a room group and returns the room history
    /// (last 50 messages, oldest first). Also invoked on reconnect and when
    /// switching rooms. A previously unknown (valid) name creates the room
    /// and announces it to every connected client.
    /// </summary>
    public async Task<IReadOnlyList<ChatMessageDto>> JoinRoom(string roomName)
    {
        var normalized = RoomService.Normalize(roomName)
            ?? throw new HubException(
                "Invalid room name. Use 1-30 lower-case letters, digits, '-' or '_'.");

        if (await _rooms.EnsureRoomAsync(normalized))
        {
            await Clients.All.SendAsync("RoomCreated", normalized);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, normalized);

        var history = await _messages.GetRecentMessagesAsync(normalized);
        return history.Select(ChatMessageDto.FromEntity).ToList();
    }

    /// <summary>Called when the user switches rooms so old-room broadcasts stop arriving.</summary>
    public Task LeaveRoom(string roomName)
    {
        var normalized = RoomService.Normalize(roomName);
        return normalized is null
            ? Task.CompletedTask
            : Groups.RemoveFromGroupAsync(Context.ConnectionId, normalized);
    }

    public async Task SendMessage(string roomName, string text)
    {
        roomName = RoomService.Normalize(roomName) ?? RoomService.DefaultRoom;
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
}
