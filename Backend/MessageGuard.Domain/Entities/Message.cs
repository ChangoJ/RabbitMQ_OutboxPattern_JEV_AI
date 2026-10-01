using MessageGuard.Domain.Enums;
using MessageGuard.Domain.Events;

namespace MessageGuard.Domain.Entities;

public class Message
{
    private readonly List<object> _domainEvents = new();

    public Guid Id { get; private set; }
    public string SenderEmail { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public MessageStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Estadísticas de clasificación — null hasta que Jev o el clasificador responde
    public double? Confidence { get; private set; }
    public string? Probabilities { get; private set; } // JSON: {"spam":0.02,"normal":0.87,...}

    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    private Message() { }

    public static Message Create(string senderEmail, string content)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            SenderEmail = senderEmail,
            Content = content,
            Status = MessageStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        message._domainEvents.Add(new MessageSubmitted(Guid.NewGuid(), message.Id, senderEmail, content, message.CreatedAt));
        return message;
    }

    public void Approve(ClassificationResult result)
    {
        Status = result.Classification == Classification.Urgent
            ? MessageStatus.SentUrgent
            : MessageStatus.Sent;

        Confidence = result.Confidence;
        Probabilities = SerializeProbabilities(result.Probabilities);

        _domainEvents.Add(new MessageApproved(Guid.NewGuid(), Id, result.Classification == Classification.Urgent, DateTime.UtcNow));
    }

    public void Reject(ClassificationResult result)
    {
        Status = result.Classification switch
        {
            Classification.Spam   => MessageStatus.RejectedSpam,
            Classification.Threat => MessageStatus.RejectedThreat,
            _                     => MessageStatus.RejectedReview
        };

        Confidence = result.Confidence;
        Probabilities = SerializeProbabilities(result.Probabilities);

        _domainEvents.Add(new MessageRejected(Guid.NewGuid(), Id, result.Classification.ToString(), DateTime.UtcNow));
    }

    public void MarkForReview()
    {
        Status = MessageStatus.RejectedReview;
        _domainEvents.Add(new MessageRejected(Guid.NewGuid(), Id, "MaxRetriesExceeded", DateTime.UtcNow));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private static string SerializeProbabilities(IReadOnlyDictionary<string, double> probs)
    {
        // Construir manualmente con InvariantCulture para que el punto decimal sea siempre '.'
        var entries = probs.Select(kv =>
            $"\"{kv.Key}\":{kv.Value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}");
        return "{" + string.Join(",", entries) + "}";
    }
}
