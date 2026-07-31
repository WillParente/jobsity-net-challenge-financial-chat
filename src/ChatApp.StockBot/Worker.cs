namespace ChatApp.StockBot;

/// <summary>
/// Bot entry point. Consumes stock quote requests and publishes replies;
/// the message broker wiring arrives with the RabbitMQ integration.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stock bot started.");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
