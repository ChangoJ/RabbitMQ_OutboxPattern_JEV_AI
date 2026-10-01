using System.Text.Json;
using MessageGuard.Application.Ports;
using MessageGuard.Domain.Entities;
using MessageGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MessageGuard.Infrastructure.Repositories;

public class PostgresMessageRepository(AppDbContext db) : IMessageRepository
{
    public async Task SaveAsync(Message message, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        db.Messages.Add(message);
        SaveOutboxEvents(message);

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        message.ClearDomainEvents();
    }

    public async Task UpdateAsync(Message message, Guid? processedEventId = null, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        SaveOutboxEvents(message);

        if (processedEventId.HasValue)
            db.ProcessedEvents.Add(ProcessedEvent.Create(processedEventId.Value));

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        message.ClearDomainEvents();
    }

    public async Task<Message?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Messages.FindAsync([id], ct);

    private void SaveOutboxEvents(Message message)
    {
        foreach (var domainEvent in message.DomainEvents)
        {
            db.OutboxMessages.Add(OutboxMessage.Create(
                eventType: domainEvent.GetType().Name,
                payload: JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
            ));
        }
    }
}
