using MessageGuard.Application.Ports;
using MessageGuard.Domain.Entities;

namespace MessageGuard.Application.UseCases;

public class SubmitMessage(IMessageRepository repository)
{
    public async Task<Message> ExecuteAsync(string senderEmail, string content, CancellationToken ct = default)
    {
        var message = Message.Create(senderEmail, content);
        await repository.SaveAsync(message, ct);
        return message;
    }
}
