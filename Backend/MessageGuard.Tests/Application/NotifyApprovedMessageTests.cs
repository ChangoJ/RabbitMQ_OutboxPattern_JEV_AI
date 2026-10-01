using MessageGuard.Application.UseCases;
using MessageGuard.Domain.Enums;
using MessageGuard.Tests.Application.Fakes;

namespace MessageGuard.Tests.Application;

public class NotifyApprovedMessageTests
{
    private static async Task<(InMemoryMessageRepository repo, FakeEmailSender email, FakeProcessedEventRepository processed, NotifyApprovedMessage useCase)> BuildAsync()
    {
        var repo = new InMemoryMessageRepository();
        var email = new FakeEmailSender();
        var processed = new FakeProcessedEventRepository();
        var useCase = new NotifyApprovedMessage(repo, email, processed);

        // Preparar un mensaje aprobado en el repo
        var submitUseCase = new SubmitMessage(repo);
        await submitUseCase.ExecuteAsync("user@test.com", "necesito ayuda");
        var message = repo.All().First();
        var classifyUseCase = new ClassifyMessage(repo, new FakeClassifier(Classification.Normal));
        await classifyUseCase.ExecuteAsync(message.Id, Guid.NewGuid());

        return (repo, email, processed, useCase);
    }

    [Fact]
    public async Task Execute_DebeEnviarCorreoAlRemitente()
    {
        var (repo, email, _, useCase) = await BuildAsync();
        var messageId = repo.All().First().Id;

        await useCase.ExecuteAsync(messageId, Guid.NewGuid(), isUrgent: false);

        Assert.Single(email.SentEmails);
        Assert.Equal("user@test.com", email.SentEmails[0].To);
    }

    [Fact]
    public async Task Execute_MensajeUrgente_DebeUsarAsuntoUrgente()
    {
        var (repo, email, _, useCase) = await BuildAsync();
        var messageId = repo.All().First().Id;

        await useCase.ExecuteAsync(messageId, Guid.NewGuid(), isUrgent: true);

        Assert.Contains("[URGENTE]", email.SentEmails[0].Subject);
    }

    [Fact]
    public async Task Execute_IdempotenciaConMismoEventId_NoDebeEnviarDosVeces()
    {
        var (repo, email, _, useCase) = await BuildAsync();
        var messageId = repo.All().First().Id;
        var eventId = Guid.NewGuid();

        await useCase.ExecuteAsync(messageId, eventId, isUrgent: false);
        await useCase.ExecuteAsync(messageId, eventId, isUrgent: false);

        Assert.Single(email.SentEmails);
    }

    [Fact]
    public async Task Execute_MensajeInexistente_NoDebeLanzarError()
    {
        var repo = new InMemoryMessageRepository();
        var useCase = new NotifyApprovedMessage(repo, new FakeEmailSender(), new FakeProcessedEventRepository());

        var exception = await Record.ExceptionAsync(() =>
            useCase.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), isUrgent: false));

        Assert.Null(exception);
    }
}
