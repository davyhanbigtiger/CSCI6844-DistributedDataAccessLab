using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace OrderService.Api.Messaging;

public class RabbitMqPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

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

        // 声明两个队列
        _channel.QueueDeclare(queue: "order.created", durable: true,
            exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueDeclare(queue: "order.cancelled", durable: true,
            exclusive: false, autoDelete: false, arguments: null);
    }

    public void Publish<T>(T evt) where T : class
    {
        var queueName = evt switch
        {
            OrderCreatedEvent   => "order.created",
            OrderCancelledEvent => "order.cancelled",
            _                   => throw new ArgumentException($"Unknown event type: {typeof(T).Name}")
        };

        var json = JsonSerializer.Serialize(evt);
        var body = Encoding.UTF8.GetBytes(json);
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;

        _channel.BasicPublish(exchange: "", routingKey: queueName,
            basicProperties: props, body: body);

        Console.WriteLine($"[Publisher] {typeof(T).Name} published to {queueName}");
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}