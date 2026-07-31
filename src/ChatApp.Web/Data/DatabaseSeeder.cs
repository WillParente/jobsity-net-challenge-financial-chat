using Microsoft.AspNetCore.Identity;

namespace ChatApp.Web.Data;

/// <summary>
/// Creates the SQLite database on startup and seeds two demo users so the
/// application can be evaluated with two browsers without registering first.
/// </summary>
public static class DatabaseSeeder
{
    public static readonly (string Username, string Password)[] DemoUsers =
    [
        ("alice", "Passw0rd!"),
        ("bob", "Passw0rd!"),
    ];

    public static void EnsureCreatedAndSeed(ChatDbContext dbContext, IPasswordHasher<AppUser> passwordHasher)
    {
        dbContext.Database.EnsureCreated();

        if (!dbContext.Rooms.Any(r => r.Name == Services.RoomService.DefaultRoom))
        {
            dbContext.Rooms.Add(new Room { Name = Services.RoomService.DefaultRoom });
        }

        foreach (var (username, password) in DemoUsers)
        {
            if (dbContext.Users.Any(u => u.Username == username))
            {
                continue;
            }

            var user = new AppUser { Username = username, PasswordHash = string.Empty };
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            dbContext.Users.Add(user);
        }

        dbContext.SaveChanges();
    }
}
