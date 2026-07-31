using ChatApp.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Web.Services;

public class UserAccountService
{
    private readonly ChatDbContext _dbContext;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public UserAccountService(ChatDbContext dbContext, IPasswordHasher<AppUser> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<(bool Succeeded, string? Error)> RegisterAsync(string username, string password)
    {
        username = username.Trim();

        if (await _dbContext.Users.AnyAsync(u => u.Username == username))
        {
            return (false, "This username is already taken.");
        }

        var user = new AppUser { Username = username, PasswordHash = string.Empty };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return (true, null);
    }

    public async Task<AppUser?> ValidateCredentialsAsync(string username, string password)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Username == username.Trim());
        if (user is null)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }
}
