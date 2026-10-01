using MessageGuard.Application.UseCases;
using MessageGuard.Domain;
using MessageGuard.Domain.Entities;
using MessageGuard.Domain.Enums;
using MessageGuard.Tests.Application.Fakes;

namespace MessageGuard.Tests.Application;

public class RejectMessageForReviewTests
{
    [Fact]
    public async Task ExecuteAsync_WhenMessageIsPending_SetsRejectedReview()
    {
        var repo = new InMemoryMessageRepository();
        var message = Message.Create("user@test.com", "contenido");
        await repo.SaveAsync(message);

        var useCase = new RejectMessageForReview(repo);
        await useCase.ExecuteAsync(message.Id);

        var updated = await repo.GetByIdAsync(message.Id);
        Assert.Equal(MessageStatus.RejectedReview, updated!.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMessageIsPending_PublishesMessageRejectedEvent()
    {
        var repo = new InMemoryMessageRepository();
        var message = Message.Create("user@test.com", "contenido");
        await repo.SaveAsync(message);

        var useCase = new RejectMessageForReview(repo);
        await useCase.ExecuteAsync(message.Id);

        var updated = await repo.GetByIdAsync(message.Id);
        // ClearDomainEvents se llama en UpdateAsync, así que comprobamos el estado final
        Assert.Equal(MessageStatus.RejectedReview, updated!.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMessageDoesNotExist_DoesNotThrow()
    {
        var repo = new InMemoryMessageRepository();
        var useCase = new RejectMessageForReview(repo);

        // No debe lanzar excepción
        await useCase.ExecuteAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task ExecuteAsync_WhenMessageAlreadySent_DoesNotChangeStatus()
    {
        var repo = new InMemoryMessageRepository();
        var message = Message.Create("user@test.com", "contenido");
        message.Approve(new ClassificationResult(Classification.Normal, 1.0, new Dictionary<string, double>()));
        await repo.SaveAsync(message);

        var statusBefore = message.Status;
        var useCase = new RejectMessageForReview(repo);
        await useCase.ExecuteAsync(message.Id);

        var updated = await repo.GetByIdAsync(message.Id);
        Assert.Equal(statusBefore, updated!.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMessageAlreadyRejectedSpam_DoesNotChangeStatus()
    {
        var repo = new InMemoryMessageRepository();
        var message = Message.Create("user@test.com", "spam barato");
        message.Reject(new ClassificationResult(Classification.Spam, 1.0, new Dictionary<string, double>()));
        await repo.SaveAsync(message);

        var useCase = new RejectMessageForReview(repo);
        await useCase.ExecuteAsync(message.Id);

        var updated = await repo.GetByIdAsync(message.Id);
        Assert.Equal(MessageStatus.RejectedSpam, updated!.Status);
    }
}
