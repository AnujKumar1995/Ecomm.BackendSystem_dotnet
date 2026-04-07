using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shared.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string exchange, string routingKey, T message) where T : class;
}

public class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;

    private RabbitMqPublisher(IConnection connection, IChannel channel, ILogger<RabbitMqPublisher> logger)
    {
        _connection = connection;
        _channel = channel;
        _logger = logger;
    }

    public static async Task<RabbitMqPublisher> CreateAsync(string hostName, ILogger<RabbitMqPublisher> logger)
    {
        var factory = new ConnectionFactory { HostName = hostName };
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        return new RabbitMqPublisher(connection, channel, logger);
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message) where T : class
    {
        await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        await _channel.BasicPublishAsync(exchange, routingKey, body);
        _logger.LogInformation("Published message to {Exchange}/{RoutingKey}: {MessageType}",
            exchange, routingKey, typeof(T).Name);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}

public interface IMessageConsumer
{
    Task SubscribeAsync<T>(string exchange, string queue, string routingKey, Func<T, Task> handler) where T : class;
}

public class RabbitMqConsumer : IMessageConsumer, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMqConsumer> _logger;

    private RabbitMqConsumer(IConnection connection, IChannel channel, ILogger<RabbitMqConsumer> logger)
    {
        _connection = connection;
        _channel = channel;
        _logger = logger;
    }

    public static async Task<RabbitMqConsumer> CreateAsync(string hostName, ILogger<RabbitMqConsumer> logger)
    {
        var factory = new ConnectionFactory { HostName = hostName };
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        return new RabbitMqConsumer(connection, channel, logger);
    }

    public async Task SubscribeAsync<T>(string exchange, string queue, string routingKey, Func<T, Task> handler) where T : class
    {
        await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);
        await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);
        await _channel.QueueBindAsync(queue, exchange, routingKey);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            _logger.LogInformation("Received message from {Queue}: {Json}", queue, json);

            try
            {
                var message = JsonSerializer.Deserialize<T>(json);
                if (message != null)
                {
                    await handler(message);
                }
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {Queue}", queue);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _channel.BasicConsumeAsync(queue, autoAck: false, consumer);
        _logger.LogInformation("Subscribed to {Exchange}/{Queue} with routing key {RoutingKey}", exchange, queue, routingKey);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}
