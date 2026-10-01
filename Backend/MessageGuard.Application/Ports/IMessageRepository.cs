using MessageGuard.Domain.Entities;

namespace MessageGuard.Application.Ports;

public interface IMessageRepository
{
    Task SaveAsync(Message message, CancellationToken ct = default);
    Task UpdateAsync(Message message, Guid? processedEventId = null, CancellationToken ct = default);
    Task<Message?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
