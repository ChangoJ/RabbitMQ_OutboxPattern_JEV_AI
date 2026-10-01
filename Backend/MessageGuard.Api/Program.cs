using MessageGuard.Application.Ports;
using MessageGuard.Application.UseCases;
using MessageGuard.Infrastructure.Classifiers;
using MessageGuard.Infrastructure.Mail;
using MessageGuard.Infrastructure.Messaging;
using MessageGuard.Infrastructure.Persistence;
using MessageGuard.Infrastructure.Repositories;
using MessageGuard.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p =>
        p.WithOrigins("http://localhost:3000", "http://localhost:3001")
         .AllowAnyHeader()
         .AllowAnyMethod()));

// Base de datos
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
        .UseSnakeCaseNamingConvention());

// Puertos y adaptadores
builder.Services.AddScoped<IMessageRepository, PostgresMessageRepository>();
builder.Services.AddScoped<IEmailSender, MailpitEmailSender>();
builder.Services.AddScoped<IProcessedEventRepository, PostgresProcessedEventRepository>();

// Clasificador: usa Jev si hay ApiKey configurada, de lo contrario usa palabras clave
builder.Services.AddHttpClient<JevClassifier>();
var useJev = !string.IsNullOrEmpty(builder.Configuration["Jev:ApiKey"]);
if (useJev)
    builder.Services.AddScoped<IMessageClassifier, JevClassifier>();
else
    builder.Services.AddScoped<IMessageClassifier, KeywordClassifier>();

// RabbitMQ
builder.Services.AddSingleton<IEventPublisher>(_ =>
    RabbitMqEventPublisher.CreateAsync(
        builder.Configuration["RabbitMq:Host"] ?? "localhost"
    ).GetAwaiter().GetResult());

// Casos de uso
builder.Services.AddScoped<SubmitMessage>();
builder.Services.AddScoped<ClassifyMessage>();
builder.Services.AddScoped<NotifyApprovedMessage>();
builder.Services.AddScoped<RejectMessageForReview>();

// Workers en segundo plano
builder.Services.AddHostedService<OutboxRelayWorker>();
builder.Services.AddHostedService<ClassifyMessageConsumer>();
builder.Services.AddHostedService<SendApprovedMessageConsumer>();
builder.Services.AddHostedService<DeadLetterConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

// POST /api/messages — enviar un mensaje nuevo
app.MapPost("/api/messages", async (SubmitMessageRequest req, SubmitMessage useCase) =>
{
    var message = await useCase.ExecuteAsync(req.SenderEmail, req.Content);
    return Results.Ok(MessageResponse.From(message));
});

// GET /api/messages/{id} — consultar el estado de un mensaje
app.MapGet("/api/messages/{id:guid}", async (Guid id, IMessageRepository repo) =>
{
    var message = await repo.GetByIdAsync(id);
    if (message is null) return Results.NotFound();
    return Results.Ok(MessageResponse.From(message));
});

app.Run();

record SubmitMessageRequest(string SenderEmail, string Content);

record MessageResponse(Guid Id, string Status, string SenderEmail, DateTime CreatedAt, double? Confidence, string? Probabilities)
{
    public static MessageResponse From(MessageGuard.Domain.Entities.Message m) =>
        new(m.Id, m.Status.ToString(), m.SenderEmail, m.CreatedAt, m.Confidence, m.Probabilities);
}
