using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProductService.Api.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ProductService.Api.Messaging;

public class OrderCreatedConsumer : BackgroundService
{
    private const string QueueName = "order.created";

    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;

    private IConnection? _connection;
    private IModel? _channel;

    public OrderCreatedConsumer(IConfiguration config, IServiceScopeFactory scopeFactory)
    {
        _config = config;
        _scopeFactory = scopeFactory;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = _config["RabbitMQ:Host"] ?? _config["RabbitMQ__Host"] ?? "localhost";

        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = "guest",
            Password = "guest",
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(json);

                if (evt is null)
                {
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    return;
                }

                Console.WriteLine($"[Consumer] order.created received: OrderId={evt.OrderId}, ProductId={evt.ProductId}, Qty={evt.Quantity}");

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

                var product = await db.Products.FirstOrDefaultAsync(p => p.Id == evt.ProductId, stoppingToken);
                if (product is not null)
                {
                    product.Stock -= evt.Quantity;
                    if (product.Stock < 0) product.Stock = 0;

                    await db.SaveChangesAsync(stoppingToken);

                    Console.WriteLine($"[Consumer] Stock updated: ProductId={product.Id}, NewStock={product.Stock}");
                }
                else
                {
                    Console.WriteLine($"[Consumer] Product not found: ProductId={evt.ProductId}");
                }

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Consumer] Error: " + ex.Message);

                // 不 ack，让消息留在队列里（简单策略，考试够用）
                // 也可以改成 BasicNack(..., requeue:true)
            }
        };

        _channel.BasicConsume(
            queue: QueueName,
            autoAck: false,
            consumer: consumer);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        try { _channel?.Close(); } catch { }
        try { _connection?.Close(); } catch { }
        base.Dispose();
    }
}
