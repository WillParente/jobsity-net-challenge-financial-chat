using RabbitMQ.Client;

namespace ChatApp.Contracts.Messaging;

/// <summary>
/// Single source of truth for the RabbitMQ topology shared by the web app
/// and the stock bot. Declarations are idempotent, so both processes declare
/// everything on startup and no startup order is required between them.
/// </summary>
public static class BrokerTopology
{
    public const string Exchange = "financial-chat";

    public const string StockRequestQueue = "stock-quote-requests";
    public const string StockRequestRoutingKey = "stock.request";

    public const string StockReplyQueue = "stock-quote-replies";
    public const string StockReplyRoutingKey = "stock.reply";

    public static void Declare(IModel channel)
    {
        channel.ExchangeDeclare(Exchange, ExchangeType.Direct, durable: true);

        channel.QueueDeclare(StockRequestQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(StockRequestQueue, Exchange, StockRequestRoutingKey);

        channel.QueueDeclare(StockReplyQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(StockReplyQueue, Exchange, StockReplyRoutingKey);
    }
}
