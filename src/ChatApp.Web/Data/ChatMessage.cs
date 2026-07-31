namespace ChatApp.Web.Data;

public class ChatMessage
{
    public long Id { get; set; }
    public required string RoomName { get; set; }
    public required string AuthorName { get; set; }
    public required string Content { get; set; }
    public DateTime TimestampUtc { get; set; }
}
