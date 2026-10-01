using MessageGuard.Application.Ports;
using MessageGuard.Domain.Enums;

namespace MessageGuard.Application.UseCases;

public class ClassifyMessage(IMessageRepository repository, IMessageClassifier classifier)
{
    public async Task ExecuteAsync(Guid messageId, Guid processedEventId, CancellationToken ct = default)
    {
        var message = await repository.GetByIdAsync(messageId, ct);

        if (message is null || message.Status != MessageStatus.Pending)
            return;

        var result = await classifier.ClassifyAsync(message.Content, ct);

        if (result.Classification is Classification.Spam or Classification.Threat)
            message.Reject(result);
        else
            message.Approve(result);

        await repository.UpdateAsync(message, processedEventId, ct);
    }
}
