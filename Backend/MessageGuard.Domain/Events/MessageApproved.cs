namespace MessageGuard.Domain.Events;

public record MessageApproved(Guid EventId, Guid MessageId, bool IsUrgent, DateTime OccurredAt);
