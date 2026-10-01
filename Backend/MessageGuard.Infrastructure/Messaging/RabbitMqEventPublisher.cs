using System.Text;
using MessageGuard.Application.Ports;
using RabbitMQ.Client;

namespace MessageGuard.Infrastructure.Messaging;

public class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    private RabbitMqEventPublisher(IConnection connection, IChannel channel)
    {
        _connection = connection;
        _channel = channel;
    }

    public static async Task<RabbitMqEventPublisher> CreateAsync(string host)
    {
        var factory = new ConnectionFactory { HostName = host };
        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        return new RabbitMqEventPublisher(connection, channel);
    }

    public async Task PublishAsync(string eventType, string payload, CancellationToken ct = default)
    {
        await _channel.ExchangeDeclareAsync(
            exchange: "messageguard.events",
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: ct);

        var body = Encoding.UTF8.GetBytes(payload);

        await _channel.BasicPublishAsync(
            exchange: "messageguard.events",
            routingKey: eventType,
            body: body,
            cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
