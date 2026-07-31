using System.Text.RegularExpressions;
using ChatApp.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Web.Services;

public partial class RoomService
{
    public const string DefaultRoom = "general";

    [GeneratedRegex("^[a-z0-9][a-z0-9_-]{0,29}$")]
    private static partial Regex ValidRoomName();

    private readonly ChatDbContext _dbContext;

    public RoomService(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<string>> GetRoomNamesAsync() =>
        await _dbContext.Rooms.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => r.Name)
            .ToListAsync();

    public static string? Normalize(string? roomName)
    {
        roomName = roomName?.Trim().ToLowerInvariant() ?? string.Empty;
        return ValidRoomName().IsMatch(roomName) ? roomName : null;
    }

    /// <summary>
    /// Creates the room if it does not exist yet. Returns true when a new
    /// room was actually created.
    /// </summary>
    public async Task<bool> EnsureRoomAsync(string normalizedName)
    {
        if (await _dbContext.Rooms.AnyAsync(r => r.Name == normalizedName))
        {
            return false;
        }

        try
        {
            _dbContext.Rooms.Add(new Room { Name = normalizedName });
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // Two users created the same room at the same time — fine.
            return false;
        }
    }
}
