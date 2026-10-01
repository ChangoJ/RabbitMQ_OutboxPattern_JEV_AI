namespace MessageGuard.Application.Ports;

public interface IProcessedEventRepository
{
    Task<bool> ExistsAsync(Guid eventId, CancellationToken ct = default);
    Task MarkAsync(Guid eventId, CancellationToken ct = default);
}
