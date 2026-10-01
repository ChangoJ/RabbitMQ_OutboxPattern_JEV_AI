using MessageGuard.Application.Ports;

namespace MessageGuard.Tests.Application.Fakes;

public class FakeProcessedEventRepository : IProcessedEventRepository
{
    private readonly HashSet<Guid> _processed = new();

    public Task<bool> ExistsAsync(Guid eventId, CancellationToken ct = default)
        => Task.FromResult(_processed.Contains(eventId));

    public Task MarkAsync(Guid eventId, CancellationToken ct = default)
    {
        _processed.Add(eventId);
        return Task.CompletedTask;
    }
}
