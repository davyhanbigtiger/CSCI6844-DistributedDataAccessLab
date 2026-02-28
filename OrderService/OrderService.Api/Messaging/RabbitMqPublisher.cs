using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace OrderService.Api.Messaging;

public class RabbitMqPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string QueueName = "order.created";

    public RabbitMqPublisher(IConfiguration config)
    {
        var host = config["RabbitMQ:Host"] ?? "localhost";

        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = "guest",
            Password = "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare queue (idempotent - safe to call multiple times)
        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    public void Publish(OrderCreatedEvent evt)
    {
        var json = JsonSerializer.Serialize(evt);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;  // message survives RabbitMQ restart

        _channel.BasicPublish(
            exchange: "",
            routingKey: QueueName,
            basicProperties: properties,
            body: body);

        Console.WriteLine($"[Publisher] OrderCreated published: OrderId={evt.OrderId}, ProductId={evt.ProductId}, Qty={evt.Quantity}");
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
