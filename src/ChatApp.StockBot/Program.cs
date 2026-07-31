using ChatApp.Contracts.Messaging;
using ChatApp.StockBot;
using ChatApp.StockBot.Stooq;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection(RabbitMqSettings.SectionName));

builder.Services.AddHttpClient<IStooqClient, StooqClient>((provider, client) =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(configuration["Stooq:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(configuration.GetValue("Stooq:TimeoutSeconds", 5));
});

builder.Services.AddSingleton<StockQuoteProcessor>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
