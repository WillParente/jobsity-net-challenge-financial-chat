using ChatApp.Web.Data;
using ChatApp.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Tests;

/// <summary>
/// Runs against SQLite in-memory (a real relational provider, unlike the
/// EF InMemory provider) so OrderBy/Take semantics go through actual SQL.
/// </summary>
public sealed class ChatMessageServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ChatDbContext _dbContext;
    private readonly ChatMessageService _service;

    public ChatMessageServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new ChatDbContext(options);
        _dbContext.Database.EnsureCreated();
        _service = new ChatMessageService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private void Seed(string room, int count, DateTime start)
    {
        for (var i = 1; i <= count; i++)
        {
            _dbContext.Messages.Add(new ChatMessage
            {
                RoomName = room,
                AuthorName = "alice",
                Content = $"message {i}",
                TimestampUtc = start.AddSeconds(i),
            });
        }

        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetRecent_MoreThan50Exist_ReturnsExactly50()
    {
        Seed("general", 60, DateTime.UtcNow.AddMinutes(-10));

        var history = await _service.GetRecentMessagesAsync("general");

        Assert.Equal(ChatMessageService.HistoryLimit, history.Count);
    }

    [Fact]
    public async Task GetRecent_MoreThan50Exist_DropsTheOldestMessages()
    {
        Seed("general", 60, DateTime.UtcNow.AddMinutes(-10));

        var history = await _service.GetRecentMessagesAsync("general");

        // 60 seeded, 50 kept: messages 11..60 survive, 1..10 are dropped.
        Assert.Equal("message 11", history.First().Content);
        Assert.Equal("message 60", history.Last().Content);
    }

    [Fact]
    public async Task GetRecent_ReturnsChronologicalOrderForDisplay()
    {
        var now = DateTime.UtcNow;

        // Inserted deliberately out of order.
        foreach (var offset in new[] { 30, 10, 50, 20, 40 })
        {
            _dbContext.Messages.Add(new ChatMessage
            {
                RoomName = "general",
                AuthorName = "alice",
                Content = $"offset {offset}",
                TimestampUtc = now.AddSeconds(offset),
            });
        }

        _dbContext.SaveChanges();

        var history = await _service.GetRecentMessagesAsync("general");

        Assert.Equal(
            new[] { "offset 10", "offset 20", "offset 30", "offset 40", "offset 50" },
            history.Select(m => m.Content));
    }

    [Fact]
    public async Task GetRecent_FewerThan50Exist_ReturnsAll()
    {
        Seed("general", 5, DateTime.UtcNow.AddMinutes(-10));

        var history = await _service.GetRecentMessagesAsync("general");

        Assert.Equal(5, history.Count);
    }

    [Fact]
    public async Task GetRecent_SameTimestamp_KeepsInsertionOrder()
    {
        var timestamp = DateTime.UtcNow;
        for (var i = 1; i <= 3; i++)
        {
            _dbContext.Messages.Add(new ChatMessage
            {
                RoomName = "general",
                AuthorName = "alice",
                Content = $"tied {i}",
                TimestampUtc = timestamp,
            });
        }

        _dbContext.SaveChanges();

        var history = await _service.GetRecentMessagesAsync("general");

        Assert.Equal(new[] { "tied 1", "tied 2", "tied 3" }, history.Select(m => m.Content));
    }

    [Fact]
    public async Task GetRecent_OnlyReturnsMessagesOfTheRequestedRoom()
    {
        Seed("general", 3, DateTime.UtcNow.AddMinutes(-10));
        Seed("finance", 2, DateTime.UtcNow.AddMinutes(-5));

        var history = await _service.GetRecentMessagesAsync("finance");

        Assert.Equal(2, history.Count);
        Assert.All(history, m => Assert.Equal("finance", m.RoomName));
    }

    [Fact]
    public async Task AddMessage_StampsUtcTimestamp()
    {
        var saved = await _service.AddMessageAsync("general", "alice", "hello");

        Assert.Equal(DateTimeKind.Utc, saved.TimestampUtc.Kind);
        Assert.True((DateTime.UtcNow - saved.TimestampUtc).Duration() < TimeSpan.FromSeconds(5));
    }
}
