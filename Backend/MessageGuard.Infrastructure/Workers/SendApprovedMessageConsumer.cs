using System.Text;
using System.Text.Json;
using MessageGuard.Application.UseCases;
using MessageGuard.Domain.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MessageGuard.Infrastructure.Workers;

public class SendApprovedMessageConsumer(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SendApprovedMessageConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = configuration["RabbitMq:Host"] ?? "localhost";
        var factory = new ConnectionFactory { HostName = host };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync("messageguard.dlx", ExchangeType.Fanout, durable: true, cancellationToken: stoppingToken);
        await channel.ExchangeDeclareAsync("messageguard.events", ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: "messageguard.notify",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = "messageguard.dlx" },
            cancellationToken: stoppingToken);
        await channel.QueueBindAsync("messageguard.notify", "messageguard.events", "MessageApproved", cancellationToken: stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.Span);
                var evt = JsonSerializer.Deserialize<MessageApproved>(json);

                if (evt is null)
                {
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                using var scope = scopeFactory.CreateScope();
                var useCase = scope.ServiceProvider.GetRequiredService<NotifyApprovedMessage>();

                // evt.EventId distinto al EventId del MessageSubmitted — no choca con processed_events
                await useCase.ExecuteAsync(evt.MessageId, evt.EventId, evt.IsUrgent, stoppingToken);

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                logger.LogInformation("Correo enviado para mensaje: {MessageId}", evt.MessageId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error enviando correo. DeliveryTag: {Tag}", ea.DeliveryTag);
                // nack sin reencolar: el DLX gestiona el retry
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await channel.BasicConsumeAsync("messageguard.notify", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
