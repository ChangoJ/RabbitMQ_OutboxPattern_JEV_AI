using MessageGuard.Application.Ports;
using MessageGuard.Domain.Enums;

namespace MessageGuard.Application.UseCases;

public class RejectMessageForReview(IMessageRepository repository)
{
    public async Task ExecuteAsync(Guid messageId, CancellationToken ct = default)
    {
        var message = await repository.GetByIdAsync(messageId, ct);

        // Si no existe o ya fue procesado por otra vía, no hacemos nada
        if (message is null || message.Status != MessageStatus.Pending)
            return;

        message.MarkForReview();
        await repository.UpdateAsync(message, processedEventId: null, ct);
    }
}
