namespace MessageGuard.Domain.Enums;

public enum MessageStatus
{
    Pending,
    RejectedSpam,
    RejectedThreat,
    RejectedReview,
    Sent,
    SentUrgent
}
