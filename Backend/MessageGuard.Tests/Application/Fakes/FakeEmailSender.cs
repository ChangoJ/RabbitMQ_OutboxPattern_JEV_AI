using MessageGuard.Application.Ports;

namespace MessageGuard.Tests.Application.Fakes;

public class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> SentEmails { get; } = new();

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        SentEmails.Add((to, subject, body));
        return Task.CompletedTask;
    }
}
