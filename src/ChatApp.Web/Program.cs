using ChatApp.Web.Data;
using ChatApp.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ChatDb")));

builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<UserAccountService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    DatabaseSeeder.EnsureCreatedAndSeed(
        scope.ServiceProvider.GetRequiredService<ChatDbContext>(),
        scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>());
}

app.Run();
