using MessageGuard.Application.Ports;

namespace MessageGuard.Application.UseCases;

public class NotifyApprovedMessage(
    IMessageRepository messageRepo,
    IEmailSender emailSender,
    IProcessedEventRepository processedEvents)
{
    public async Task ExecuteAsync(Guid messageId, Guid processedEventId, bool isUrgent, CancellationToken ct = default)
    {
        if (await processedEvents.ExistsAsync(processedEventId, ct))
            return;

        var message = await messageRepo.GetByIdAsync(messageId, ct);
        if (message is null) return;

        var subject = isUrgent
            ? "[URGENTE] Tu mensaje fue recibido con prioridad"
            : "Tu mensaje fue recibido";

        var body = $"Hola,\n\nTu mensaje ha sido procesado y enviado.\n\nContenido: {message.Content}";

        await emailSender.SendAsync(message.SenderEmail, subject, body, ct);
        await processedEvents.MarkAsync(processedEventId, ct);
    }
}
