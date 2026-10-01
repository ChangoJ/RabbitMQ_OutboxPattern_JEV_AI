namespace MessageGuard.Domain.Entities;

public class ProcessedEvent
{
    public Guid Id { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    private ProcessedEvent() { }

    public static ProcessedEvent Create(Guid id) => new()
    {
        Id = id,
        ProcessedAt = DateTime.UtcNow
    };
}
