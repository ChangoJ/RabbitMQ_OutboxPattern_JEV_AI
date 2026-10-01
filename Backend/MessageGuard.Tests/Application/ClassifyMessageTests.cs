using MessageGuard.Application.UseCases;
using MessageGuard.Domain.Enums;
using MessageGuard.Tests.Application.Fakes;

namespace MessageGuard.Tests.Application;

public class ClassifyMessageTests
{
    private static async Task<(InMemoryMessageRepository repo, ClassifyMessage useCase)> BuildAsync(Classification clasificacion)
    {
        var repo = new InMemoryMessageRepository();
        var submitUseCase = new SubmitMessage(repo);
        await submitUseCase.ExecuteAsync("user@test.com", "contenido");

        var classifier = new FakeClassifier(clasificacion);
        var classifyUseCase = new ClassifyMessage(repo, classifier);
        return (repo, classifyUseCase);
    }

    [Fact]
    public async Task Execute_MensajeNormal_DebeQuedarSent()
    {
        var (repo, useCase) = await BuildAsync(Classification.Normal);
        var messageId = repo.All().First().Id;

        await useCase.ExecuteAsync(messageId, Guid.NewGuid());

        var message = await repo.GetByIdAsync(messageId);
        Assert.Equal(MessageStatus.Sent, message!.Status);
    }

    [Fact]
    public async Task Execute_MensajeUrgente_DebeQuedarSentUrgent()
    {
        var (repo, useCase) = await BuildAsync(Classification.Urgent);
        var messageId = repo.All().First().Id;

        await useCase.ExecuteAsync(messageId, Guid.NewGuid());

        var message = await repo.GetByIdAsync(messageId);
        Assert.Equal(MessageStatus.SentUrgent, message!.Status);
    }

    [Fact]
    public async Task Execute_MensajeSpam_DebeQuedarRejectedSpam()
    {
        var (repo, useCase) = await BuildAsync(Classification.Spam);
        var messageId = repo.All().First().Id;

        await useCase.ExecuteAsync(messageId, Guid.NewGuid());

        var message = await repo.GetByIdAsync(messageId);
        Assert.Equal(MessageStatus.RejectedSpam, message!.Status);
    }

    [Fact]
    public async Task Execute_IdempotenciaConMismoEventId_NoDebeReprocesar()
    {
        var (repo, useCase) = await BuildAsync(Classification.Normal);
        var messageId = repo.All().First().Id;
        var eventId = Guid.NewGuid();

        // Primera vez: clasifica
        await useCase.ExecuteAsync(messageId, eventId);
        // Segunda vez con el mismo eventId: el mensaje ya no está Pending, no hace nada
        await useCase.ExecuteAsync(messageId, eventId);

        var message = await repo.GetByIdAsync(messageId);
        Assert.Equal(MessageStatus.Sent, message!.Status);
    }

    [Fact]
    public async Task Execute_MensajeInexistente_NoDebeLanzarError()
    {
        var repo = new InMemoryMessageRepository();
        var useCase = new ClassifyMessage(repo, new FakeClassifier(Classification.Normal));

        var exception = await Record.ExceptionAsync(() =>
            useCase.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Null(exception);
    }
}
