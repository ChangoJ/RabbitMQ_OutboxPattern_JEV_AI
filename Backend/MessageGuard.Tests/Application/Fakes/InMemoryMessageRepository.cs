using MessageGuard.Application.Ports;
using MessageGuard.Domain.Entities;

namespace MessageGuard.Tests.Application.Fakes;

public class InMemoryMessageRepository : IMessageRepository
{
    private readonly Dictionary<Guid, Message> _store = new();
    private readonly HashSet<Guid> _processedEvents = new();

    public Task SaveAsync(Message message, CancellationToken ct = default)
    {
        _store[message.Id] = message;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Message message, Guid? processedEventId = null, CancellationToken ct = default)
    {
        _store[message.Id] = message;
        if (processedEventId.HasValue) _processedEvents.Add(processedEventId.Value);
        message.ClearDomainEvents();
        return Task.CompletedTask;
    }

    public Task<Message?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _store.TryGetValue(id, out var message);
        return Task.FromResult(message);
    }

    public bool WasProcessed(Guid eventId) => _processedEvents.Contains(eventId);
    public IEnumerable<Message> All() => _store.Values;
}
