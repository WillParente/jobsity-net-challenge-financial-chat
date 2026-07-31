using ChatApp.Contracts.Messaging;
using ChatApp.Web.Data;
using ChatApp.Web.Hubs;
using ChatApp.Web.Messaging;
using ChatApp.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddSignalR();

builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ChatDb")));

builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<UserAccountService>();
builder.Services.AddScoped<ChatMessageService>();
builder.Services.AddScoped<RoomService>();

builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection(RabbitMqSettings.SectionName));
builder.Services.AddSingleton<IStockRequestPublisher, RabbitMqStockRequestPublisher>();
builder.Services.AddHostedService<StockQuoteReplyConsumer>();

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
app.MapHub<ChatHub>("/hubs/chat");

using (var scope = app.Services.CreateScope())
{
    DatabaseSeeder.EnsureCreatedAndSeed(
        scope.ServiceProvider.GetRequiredService<ChatDbContext>(),
        scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>());
}

app.Run();
