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

    public ChatHub(ChatMessageService messages)
    {
        _messages = messages;
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

        var saved = await _messages.AddMessageAsync(roomName, CurrentUsername, text);
        await Clients.Group(roomName).SendAsync("ReceiveMessage", ChatMessageDto.FromEntity(saved));
    }

    private static string NormalizeRoom(string roomName) =>
        string.IsNullOrWhiteSpace(roomName) ? DefaultRoom : roomName.Trim().ToLowerInvariant();
}
