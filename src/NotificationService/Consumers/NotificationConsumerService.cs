using Shared.Events;
using Shared.Messaging;

namespace NotificationService.Consumers;

public class NotificationConsumerService : BackgroundService
{
    private readonly ILogger<NotificationConsumerService> _logger;
    private readonly string _rabbitHost;

    public NotificationConsumerService(ILogger<NotificationConsumerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _rabbitHost = configuration["RabbitMQ:Host"] ?? "rabbitmq";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Consumer Service is starting...");

        // Retry connection to RabbitMQ
        RabbitMqConsumer? consumer = null;
        for (int i = 0; i < 10; i++)
        {
            try
            {
                consumer = await RabbitMqConsumer.CreateAsync(_rabbitHost,
                    _logger as ILogger<RabbitMqConsumer> ?? LoggerFactory.Create(b => b.AddConsole()).CreateLogger<RabbitMqConsumer>());
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("RabbitMQ not ready, retrying in 5 seconds... ({Attempt}/10) - {Error}", i + 1, ex.Message);
                await Task.Delay(5000, stoppingToken);
            }
        }

        if (consumer == null)
        {
            _logger.LogError("Could not connect to RabbitMQ after 10 attempts. Notification service will run without messaging.");
            return;
        }

        // Subscribe to order created events
        await consumer.SubscribeAsync<OrderCreatedEvent>(
            "ecommerce.events",
            "notification.order.created",
            "order.created",
            async (evt) =>
            {
                _logger.LogInformation("========================================");
                _logger.LogInformation("NOTIFICATION: Order Created!");
                _logger.LogInformation("  Order ID:    {OrderId}", evt.OrderId);
                _logger.LogInformation("  User ID:     {UserId}", evt.UserId);
                _logger.LogInformation("  Total:       ${Total:F2}", evt.TotalAmount);
                _logger.LogInformation("  Items:       {ItemCount}", evt.ItemCount);
                _logger.LogInformation("  Timestamp:   {Timestamp}", evt.Timestamp);
                _logger.LogInformation("  Message:     Your order has been placed successfully!");
                _logger.LogInformation("========================================");
                await Task.CompletedTask;
            });

        // Subscribe to order failed events
        await consumer.SubscribeAsync<OrderFailedEvent>(
            "ecommerce.events",
            "notification.order.failed",
            "order.failed",
            async (evt) =>
            {
                _logger.LogInformation("========================================");
                _logger.LogInformation("NOTIFICATION: Order Failed!");
                _logger.LogInformation("  Order ID:    {OrderId}", evt.OrderId);
                _logger.LogInformation("  User ID:     {UserId}", evt.UserId);
                _logger.LogInformation("  Reason:      {Reason}", evt.Reason);
                _logger.LogInformation("  Timestamp:   {Timestamp}", evt.Timestamp);
                _logger.LogInformation("  Message:     Sorry, your order could not be processed.");
                _logger.LogInformation("========================================");
                await Task.CompletedTask;
            });

        _logger.LogInformation("Notification Consumer Service is now listening for events...");

        // Keep alive
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await consumer.DisposeAsync();
    }
}
