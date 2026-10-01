namespace MessageGuard.Domain.Events;

public record MessageRejected(Guid EventId, Guid MessageId, string Reason, DateTime OccurredAt);
