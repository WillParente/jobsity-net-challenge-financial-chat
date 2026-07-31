using ChatApp.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Web.Services;

public class ChatMessageService
{
    /// <summary>Maximum number of messages shown in a chatroom (challenge requirement).</summary>
    public const int HistoryLimit = 50;

    private readonly ChatDbContext _dbContext;

    public ChatMessageService(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ChatMessage> AddMessageAsync(string roomName, string authorName, string content)
    {
        var message = new ChatMessage
        {
            RoomName = roomName,
            AuthorName = authorName,
            Content = content,
            TimestampUtc = DateTime.UtcNow,
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();
        return message;
    }

    /// <summary>
    /// Returns the newest <see cref="HistoryLimit"/> messages of a room in
    /// chronological order (oldest first), ready for display.
    /// </summary>
    public async Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(string roomName)
    {
        var newestFirst = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.RoomName == roomName)
            .OrderByDescending(m => m.TimestampUtc)
            .ThenByDescending(m => m.Id) // stable tie-break for same-timestamp messages
            .Take(HistoryLimit)
            .ToListAsync();

        newestFirst.Reverse();
        return newestFirst;
    }
}
