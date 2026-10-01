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

/// <summary>
/// Lee los mensajes que fallaron (dead-lettered) y decide:
///   - Si aún tiene intentos: republica al exchange original.
///   - Si ya agotó los reintentos: marca el mensaje como REJECTED_REVIEW.
/// </summary>
public class DeadLetterConsumer(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<DeadLetterConsumer> logger) : BackgroundService
{
    private const string DeadLetterExchange = "messageguard.dlx";
    private const string DeadQueue = "messageguard.dead";
    private const string MainExchange = "messageguard.events";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = configuration["RabbitMq:Host"] ?? "localhost";
        var factory = new ConnectionFactory { HostName = host };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Declara el DLX fanout: todos los muertos aterrizan aquí
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Fanout, durable: true, cancellationToken: stoppingToken);

        // Cola única que recibe todos los muertos (sin routing key porque es fanout)
        await channel.QueueDeclareAsync(DeadQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(DeadQueue, DeadLetterExchange, routingKey: "", cancellationToken: stoppingToken);

        // También declara el exchange principal para poder republicar
        await channel.ExchangeDeclareAsync(MainExchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var deathCount = RetryHelper.GetDeathCount(ea);

                if (RetryHelper.ShouldSendToDeadLetter(ea))
                {
                    // Agotó los reintentos → intentar marcar como REJECTED_REVIEW
                    logger.LogWarning("Mensaje agotó {Count} reintentos. Marcando para revisión.", deathCount);
                    await TryRejectForReviewAsync(ea.Body, ea.RoutingKey, stoppingToken);
                }
                else
                {
                    // Aún tiene intentos → republicar al exchange original con la misma routing key
                    logger.LogInformation("Reintento {Count} para routing key '{Key}'", deathCount + 1, ea.RoutingKey);
                    await channel.BasicPublishAsync(
                        exchange: MainExchange,
                        routingKey: ea.RoutingKey,
                        body: ea.Body,
                        cancellationToken: stoppingToken);
                }

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en DeadLetterConsumer. DeliveryTag: {Tag}", ea.DeliveryTag);
                // Ack igual: no queremos un bucle infinito en la cola de muertos
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
        };

        await channel.BasicConsumeAsync(DeadQueue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task TryRejectForReviewAsync(ReadOnlyMemory<byte> body, string routingKey, CancellationToken ct)
    {
        // Solo aplica para eventos de clasificación; los de notificación (MessageApproved) no tienen mensaje que rechazar
        if (routingKey != "MessageSubmitted") return;

        var json = Encoding.UTF8.GetString(body.Span);
        var evt = JsonSerializer.Deserialize<MessageSubmitted>(json);
        if (evt is null) return;

        using var scope = scopeFactory.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<RejectMessageForReview>();
        await useCase.ExecuteAsync(evt.MessageId, ct);
    }
}
