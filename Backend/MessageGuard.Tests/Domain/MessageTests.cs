using MessageGuard.Domain;
using MessageGuard.Domain.Entities;
using MessageGuard.Domain.Enums;
using MessageGuard.Domain.Events;

namespace MessageGuard.Tests.Domain;

public class MessageTests
{
    private static ClassificationResult Result(Classification c) => new(c, 1.0,
        new Dictionary<string, double> { ["spam"] = 0, ["threat"] = 0, ["urgent"] = 0, ["normal"] = 0,
            [c.ToString().ToLower()] = 1.0 });

    [Fact]
    public void Create_DebeCrearMensajeConEstadoPending()
    {
        var message = Message.Create("user@test.com", "necesito ayuda");

        Assert.Equal(MessageStatus.Pending, message.Status);
        Assert.Equal("user@test.com", message.SenderEmail);
        Assert.Equal("necesito ayuda", message.Content);
        Assert.NotEqual(Guid.Empty, message.Id);
    }

    [Fact]
    public void Create_DebeRegistrarEventoMessageSubmitted()
    {
        var message = Message.Create("user@test.com", "necesito ayuda");

        var evento = Assert.Single(message.DomainEvents);
        Assert.IsType<MessageSubmitted>(evento);
    }

    [Fact]
    public void Approve_ConClasificacionNormal_DebeQuedarSent()
    {
        var message = Message.Create("user@test.com", "contenido");
        message.Approve(Result(Classification.Normal));
        Assert.Equal(MessageStatus.Sent, message.Status);
    }

    [Fact]
    public void Approve_ConClasificacionUrgent_DebeQuedarSentUrgent()
    {
        var message = Message.Create("user@test.com", "contenido");
        message.Approve(Result(Classification.Urgent));
        Assert.Equal(MessageStatus.SentUrgent, message.Status);
    }

    [Fact]
    public void Approve_DebeRegistrarEventoMessageApproved()
    {
        var message = Message.Create("user@test.com", "contenido");
        message.ClearDomainEvents();

        message.Approve(Result(Classification.Normal));

        var evento = Assert.Single(message.DomainEvents);
        Assert.IsType<MessageApproved>(evento);
    }

    [Fact]
    public void Approve_DebeGuardarConfianza()
    {
        var message = Message.Create("user@test.com", "contenido");
        message.Approve(Result(Classification.Normal));
        Assert.Equal(1.0, message.Confidence);
    }

    [Fact]
    public void Reject_ConSpam_DebeQuedarRejectedSpam()
    {
        var message = Message.Create("user@test.com", "contenido");
        message.Reject(Result(Classification.Spam));
        Assert.Equal(MessageStatus.RejectedSpam, message.Status);
    }

    [Fact]
    public void Reject_ConThreat_DebeQuedarRejectedThreat()
    {
        var message = Message.Create("user@test.com", "contenido");
        message.Reject(Result(Classification.Threat));
        Assert.Equal(MessageStatus.RejectedThreat, message.Status);
    }

    [Fact]
    public void Reject_DebeRegistrarEventoMessageRejected()
    {
        var message = Message.Create("user@test.com", "contenido");
        message.ClearDomainEvents();

        message.Reject(Result(Classification.Spam));

        var evento = Assert.Single(message.DomainEvents);
        Assert.IsType<MessageRejected>(evento);
    }

    [Fact]
    public void ClearDomainEvents_DebeVaciarLaLista()
    {
        var message = Message.Create("user@test.com", "contenido");
        message.ClearDomainEvents();
        Assert.Empty(message.DomainEvents);
    }
}
