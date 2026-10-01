using MessageGuard.Domain;

namespace MessageGuard.Application.Ports;

public interface IMessageClassifier
{
    Task<ClassificationResult> ClassifyAsync(string content, CancellationToken ct = default);
}
