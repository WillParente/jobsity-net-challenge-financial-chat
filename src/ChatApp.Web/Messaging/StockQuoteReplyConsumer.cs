using System.Text.Json;
using ChatApp.Contracts;
using ChatApp.Contracts.Messaging;
using ChatApp.Web.Hubs;
using ChatApp.Web.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChatApp.Web.Messaging;

/// <summary>
/// Consumes the bot's replies from RabbitMQ, persists each one as a chat
/// message owned by the bot, then broadcasts it to the room. Persisting
/// before broadcasting means a page refresh right after the reply still
/// shows it in the history.
/// </summary>
public sealed class StockQuoteReplyConsumer : BackgroundService
{
    public const string BotName = "StockBot";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<StockQuoteReplyConsumer> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public StockQuoteReplyConsumer(
        IServiceScopeFactory scopeFactory,
        IHubContext<ChatHub> hubContext,
        IOptions<RabbitMqSettings> settings,
        ILogger<StockQuoteReplyConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
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
        consumer.Received += (_, delivery) => HandleDeliveryAsync(delivery);
        _channel.BasicConsume(BrokerTopology.StockReplyQueue, autoAck: false, consumer);

        _logger.LogInformation("Web app connected to RabbitMQ, waiting for bot replies.");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleDeliveryAsync(BasicDeliverEventArgs delivery)
    {
        StockQuoteReply? reply;
        try
        {
            reply = JsonSerializer.Deserialize<StockQuoteReply>(delivery.Body.Span);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Discarding malformed stock reply message.");
            _channel!.BasicNack(delivery.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        if (reply is null || string.IsNullOrWhiteSpace(reply.Message))
        {
            _logger.LogError("Discarding empty stock reply message.");
            _channel!.BasicNack(delivery.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        try
        {
            // BackgroundService is a singleton; the DbContext is scoped, so a
            // scope is created per message.
            ChatMessageDto dto;
            using (var scope = _scopeFactory.CreateScope())
            {
                var messages = scope.ServiceProvider.GetRequiredService<ChatMessageService>();
                var saved = await messages.AddMessageAsync(reply.RoomName, BotName, reply.Message);
                dto = ChatMessageDto.FromEntity(saved);
            }

            await _hubContext.Clients.Group(reply.RoomName).SendAsync("ReceiveMessage", dto);
            _channel!.BasicAck(delivery.DeliveryTag, multiple: false);

            _logger.LogInformation("Posted bot reply {RequestId} to room {RoomName}.",
                reply.RequestId, reply.RoomName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process bot reply {RequestId}.", reply.RequestId);
            _channel!.BasicNack(delivery.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
