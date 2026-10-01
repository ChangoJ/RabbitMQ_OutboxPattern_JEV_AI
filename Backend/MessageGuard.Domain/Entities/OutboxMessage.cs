namespace MessageGuard.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public bool Published { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(string eventType, string payload) => new()
    {
        Id = Guid.NewGuid(),
        EventType = eventType,
        Payload = payload,
        CreatedAt = DateTime.UtcNow,
        Published = false
    };

    public void MarkPublished() => Published = true;
}
