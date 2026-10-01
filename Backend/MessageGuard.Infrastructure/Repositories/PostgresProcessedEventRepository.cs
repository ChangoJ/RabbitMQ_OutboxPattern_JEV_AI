using MessageGuard.Application.Ports;
using MessageGuard.Domain.Entities;
using MessageGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MessageGuard.Infrastructure.Repositories;

public class PostgresProcessedEventRepository(AppDbContext db) : IProcessedEventRepository
{
    public async Task<bool> ExistsAsync(Guid eventId, CancellationToken ct = default)
        => await db.ProcessedEvents.AnyAsync(e => e.Id == eventId, ct);

    public async Task MarkAsync(Guid eventId, CancellationToken ct = default)
    {
        db.ProcessedEvents.Add(ProcessedEvent.Create(eventId));
        await db.SaveChangesAsync(ct);
    }
}
