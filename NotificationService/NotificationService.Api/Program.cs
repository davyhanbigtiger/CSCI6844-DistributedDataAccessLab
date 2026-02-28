using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<OrderCreatedConsumer>();

var app = builder.Build();
app.MapGet("/", () => "NotificationService is running");
app.Run();

public class OrderCreatedConsumer : BackgroundService
{
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly IConfiguration _config;

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = _config["RabbitMQ__Host"] ?? "rabbitmq";
        var factory = new ConnectionFactory { HostName = host };

        IConnection? connection = null;
        for (int i = 0; i < 10; i++)
        {
            try
            {
                connection = await factory.CreateConnectionAsync(stoppingToken);
                _logger.LogInformation("✅ Connected to RabbitMQ");
                break;
            }
            catch
            {
                _logger.LogWarning("RabbitMQ not ready, retrying in 3s... ({Attempt}/10)", i + 1);
                await Task.Delay(3000, stoppingToken);
            }
        }

        if (connection == null)
        {
            _logger.LogError("❌ Could not connect to RabbitMQ after 10 attempts.");
            return;
        }

        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: "order-created",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation("📧 [NotificationService] OrderCreated received: {Message}", message);
            _logger.LogInformation("📧 Notification sent to customer!");
            await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
        };

        await channel.BasicConsumeAsync(
            queue: "order-created",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("✅ NotificationService listening on queue: order-created");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
