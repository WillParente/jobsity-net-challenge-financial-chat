using Microsoft.EntityFrameworkCore;

namespace ChatApp.Web.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(user =>
        {
            user.HasIndex(u => u.Username).IsUnique();
            user.Property(u => u.Username).HasMaxLength(30);
        });

        modelBuilder.Entity<ChatMessage>(message =>
        {
            // Serves the "last 50 messages of a room ordered by timestamp" query.
            message.HasIndex(m => new { m.RoomName, m.TimestampUtc });
            message.Property(m => m.RoomName).HasMaxLength(50);
            message.Property(m => m.AuthorName).HasMaxLength(30);
            message.Property(m => m.Content).HasMaxLength(500);

            // SQLite drops DateTimeKind on round-trip; timestamps are always
            // stored as UTC, so restore the kind when materializing.
            message.Property(m => m.TimestampUtc).HasConversion(
                utc => utc,
                stored => DateTime.SpecifyKind(stored, DateTimeKind.Utc));
        });
    }
}
