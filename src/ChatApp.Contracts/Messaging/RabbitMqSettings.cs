namespace ChatApp.Contracts.Messaging;

/// <summary>Bound from the "RabbitMq" configuration section in both processes.</summary>
public class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    /// <summary>How many connection attempts to make on startup before giving up.</summary>
    public int ConnectRetryAttempts { get; set; } = 30;

    /// <summary>Delay between startup connection attempts, in seconds.</summary>
    public int ConnectRetryDelaySeconds { get; set; } = 2;
}
