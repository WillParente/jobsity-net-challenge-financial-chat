using Microsoft.EntityFrameworkCore;

namespace ChatApp.Web.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(user =>
        {
            user.HasIndex(u => u.Username).IsUnique();
            user.Property(u => u.Username).HasMaxLength(30);
        });
    }
}
