using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace ChatApp.Contracts.Messaging;

public static class RabbitMqConnector
{
    public static ConnectionFactory CreateFactory(RabbitMqSettings settings) => new()
    {
        HostName = settings.HostName,
        Port = settings.Port,
        UserName = settings.UserName,
        Password = settings.Password,
        AutomaticRecoveryEnabled = true,
        DispatchConsumersAsync = true,
    };

    /// <summary>
    /// Connects with a retry loop so the process survives the broker starting
    /// a few seconds later than it does (typical with docker compose).
    /// </summary>
    public static async Task<IConnection> ConnectAsync(
        RabbitMqSettings settings,
        CancellationToken cancellationToken,
        Action<int, Exception>? onRetry = null)
    {
        var factory = CreateFactory(settings);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return factory.CreateConnection();
            }
            catch (BrokerUnreachableException ex) when (attempt < settings.ConnectRetryAttempts)
            {
                onRetry?.Invoke(attempt, ex);
                await Task.Delay(TimeSpan.FromSeconds(settings.ConnectRetryDelaySeconds), cancellationToken);
            }
        }
    }
}
