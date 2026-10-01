namespace MessageGuard.Domain.Events;

public record MessageSubmitted(Guid EventId, Guid MessageId, string SenderEmail, string Content, DateTime OccurredAt);
