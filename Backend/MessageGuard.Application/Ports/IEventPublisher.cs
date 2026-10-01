namespace MessageGuard.Application.Ports;

public interface IEventPublisher
{
    Task PublishAsync(string eventType, string payload, CancellationToken ct = default);
}
