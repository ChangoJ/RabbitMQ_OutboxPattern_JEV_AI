using MessageGuard.Application.Ports;
using MessageGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MessageGuard.Infrastructure.Workers;

public class OutboxRelayWorker(
    IServiceScopeFactory scopeFactory,
    IEventPublisher publisher,
    ILogger<OutboxRelayWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingEventsAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessPendingEventsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // FOR UPDATE SKIP LOCKED: toma hasta 10 filas pendientes y las bloquea.
        // Si otro worker está procesando alguna, la salta en vez de esperar.
        var pending = await db.OutboxMessages
            .FromSqlRaw("""
                SELECT * FROM outbox_messages
                WHERE published = false
                ORDER BY created_at
                LIMIT 10
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        if (pending.Count == 0)
        {
            await transaction.RollbackAsync(ct);
            return;
        }

        foreach (var outbox in pending)
        {
            try
            {
                await publisher.PublishAsync(outbox.EventType, outbox.Payload, ct);
                outbox.MarkPublished();
                logger.LogInformation("Evento publicado: {EventType} {Id}", outbox.EventType, outbox.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error publicando evento {Id}", outbox.Id);
                await transaction.RollbackAsync(ct);
                return;
            }
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
