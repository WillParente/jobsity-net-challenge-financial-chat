using ChatApp.Web.Data;

namespace ChatApp.Web.Hubs;

public record ChatMessageDto(long Id, string Author, string Content, DateTime TimestampUtc, bool Ephemeral = false)
{
    public static ChatMessageDto FromEntity(ChatMessage message) =>
        new(message.Id, message.AuthorName, message.Content, message.TimestampUtc);

    /// <summary>A live-only message that is never persisted (e.g. a /stock command echo).</summary>
    public static ChatMessageDto EphemeralMessage(string author, string content) =>
        new(0, author, content, DateTime.UtcNow, Ephemeral: true);
}
