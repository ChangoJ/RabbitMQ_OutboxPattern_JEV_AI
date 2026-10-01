using MailKit.Net.Smtp;
using MailKit.Security;
using MessageGuard.Application.Ports;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace MessageGuard.Infrastructure.Mail;

public class MailpitEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var host = configuration["Mail:Host"] ?? "localhost";
        var port = int.Parse(configuration["Mail:Port"] ?? "1025");
        var from = configuration["Mail:From"] ?? "noreply@messageguard.local";

        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(from));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = new TextPart("plain") { Text = body };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, SecureSocketOptions.None, ct);
        await smtp.SendAsync(email, ct);
        await smtp.DisconnectAsync(quit: true, ct);
    }
}
