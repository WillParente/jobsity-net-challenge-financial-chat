using ChatApp.Web.Data;

namespace ChatApp.Web.Hubs;

public record ChatMessageDto(long Id, string Author, string Content, DateTime TimestampUtc)
{
    public static ChatMessageDto FromEntity(ChatMessage message) =>
        new(message.Id, message.AuthorName, message.Content, message.TimestampUtc);
}
