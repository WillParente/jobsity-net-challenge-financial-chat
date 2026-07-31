using System.Text;
using System.Text.Json;
using ChatApp.Contracts;
using ChatApp.Contracts.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ChatApp.Web.Messaging;

public sealed class RabbitMqStockRequestPublisher : IStockRequestPublisher, IDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqStockRequestPublisher> _logger;
    private readonly object _syncRoot = new();

    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqStockRequestPublisher(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqStockRequestPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public bool TryPublish(StockQuoteRequest request)
    {
        try
        {
            lock (_syncRoot)
            {
                EnsureChannel();

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
                var properties = _channel!.CreateBasicProperties();
                properties.Persistent = true;

                _channel.BasicPublish(
                    BrokerTopology.Exchange,
                    BrokerTopology.StockRequestRoutingKey,
                    properties,
                    body);
            }

            _logger.LogInformation(
                "Published stock request {RequestId} for {StockCode} (room {RoomName}).",
                request.RequestId, request.StockCode, request.RoomName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not publish stock request for {StockCode}.", request.StockCode);
            return false;
        }
    }

    private void EnsureChannel()
    {
        if (_channel is { IsOpen: true })
        {
            return;
        }

        _channel?.Dispose();
        if (_connection is not { IsOpen: true })
        {
            _connection?.Dispose();
            _connection = RabbitMqConnector.CreateFactory(_settings).CreateConnection();
        }

        _channel = _connection.CreateModel();
        BrokerTopology.Declare(_channel);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
