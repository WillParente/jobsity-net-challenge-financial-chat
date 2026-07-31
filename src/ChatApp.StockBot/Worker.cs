using System.Text;
using System.Text.Json;
using ChatApp.Contracts;
using ChatApp.Contracts.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChatApp.StockBot;

/// <summary>
/// Bot entry point: consumes stock quote requests from RabbitMQ, resolves
/// them through <see cref="StockQuoteProcessor"/> and publishes the reply
/// back to the broker. The bot never talks to the web app directly — the
/// broker is the only link between the two processes.
/// </summary>
public class Worker : BackgroundService
{
    private readonly StockQuoteProcessor _processor;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<Worker> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public Worker(StockQuoteProcessor processor, IOptions<RabbitMqSettings> settings, ILogger<Worker> logger)
    {
        _processor = processor;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _connection = await RabbitMqConnector.ConnectAsync(
            _settings,
            stoppingToken,
            (attempt, ex) => _logger.LogWarning(
                "RabbitMQ not reachable yet (attempt {Attempt}): {Reason}. Retrying…",
                attempt, ex.Message));

        _channel = _connection.CreateModel();
        BrokerTopology.Declare(_channel);
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += (_, delivery) => HandleDeliveryAsync(delivery, stoppingToken);
        _channel.BasicConsume(BrokerTopology.StockRequestQueue, autoAck: false, consumer);

        _logger.LogInformation("Stock bot connected to RabbitMQ, waiting for /stock commands.");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleDeliveryAsync(BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        StockQuoteRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<StockQuoteRequest>(delivery.Body.Span);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Discarding malformed stock request message.");
            _channel!.BasicNack(delivery.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        if (request is null || string.IsNullOrWhiteSpace(request.StockCode))
        {
            _logger.LogError("Discarding empty stock request message.");
            _channel!.BasicNack(delivery.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        try
        {
            _logger.LogInformation(
                "Processing stock request {RequestId} for {StockCode} (room {RoomName}, requested by {RequestedBy}).",
                request.RequestId, request.StockCode, request.RoomName, request.RequestedBy);

            // Never throws: failures become friendly chat messages.
            var replyText = await _processor.BuildReplyAsync(request.StockCode, stoppingToken);

            Publish(new StockQuoteReply(request.RequestId, request.RoomName, replyText));
            _channel!.BasicAck(delivery.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            // Publishing failed (e.g. channel dropped): don't poison-loop the queue.
            _logger.LogError(ex, "Failed to publish reply for request {RequestId}.", request.RequestId);
            _channel!.BasicNack(delivery.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private void Publish(StockQuoteReply reply)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reply));
        var properties = _channel!.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(
            BrokerTopology.Exchange,
            BrokerTopology.StockReplyRoutingKey,
            properties,
            body);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
