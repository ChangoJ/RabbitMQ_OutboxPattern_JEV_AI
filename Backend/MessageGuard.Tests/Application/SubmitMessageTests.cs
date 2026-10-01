using MessageGuard.Application.UseCases;
using MessageGuard.Domain.Enums;
using MessageGuard.Tests.Application.Fakes;

namespace MessageGuard.Tests.Application;

public class SubmitMessageTests
{
    [Fact]
    public async Task Execute_DebeGuardarMensajeComoPending()
    {
        var repo = new InMemoryMessageRepository();
        var useCase = new SubmitMessage(repo);

        var message = await useCase.ExecuteAsync("user@test.com", "hola");

        Assert.Equal(MessageStatus.Pending, message.Status);
    }

    [Fact]
    public async Task Execute_DebeGuardarMensajeEnRepositorio()
    {
        var repo = new InMemoryMessageRepository();
        var useCase = new SubmitMessage(repo);

        var message = await useCase.ExecuteAsync("user@test.com", "hola");

        var guardado = await repo.GetByIdAsync(message.Id);
        Assert.NotNull(guardado);
    }

    [Fact]
    public async Task Execute_DebeRetornarMensajeConId()
    {
        var repo = new InMemoryMessageRepository();
        var useCase = new SubmitMessage(repo);

        var message = await useCase.ExecuteAsync("user@test.com", "hola");

        Assert.NotEqual(Guid.Empty, message.Id);
    }
}
