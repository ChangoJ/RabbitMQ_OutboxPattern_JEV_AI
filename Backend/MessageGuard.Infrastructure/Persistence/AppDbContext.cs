using MessageGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MessageGuard.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>(e =>
        {
            e.ToTable("messages");
            e.HasKey(m => m.Id);
            e.Property(m => m.SenderEmail).IsRequired().HasMaxLength(256);
            e.Property(m => m.Content).IsRequired();
            e.Property(m => m.Status).HasConversion<string>().HasMaxLength(32);
            e.Property(m => m.CreatedAt);
            e.Property(m => m.Confidence);
            e.Property(m => m.Probabilities);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.HasKey(o => o.Id);
            e.Property(o => o.EventType).IsRequired().HasMaxLength(128);
            e.Property(o => o.Payload).IsRequired();
            e.Property(o => o.CreatedAt);
            e.Property(o => o.Published);
        });

        modelBuilder.Entity<ProcessedEvent>(e =>
        {
            e.ToTable("processed_events");
            e.HasKey(p => p.Id);
            e.Property(p => p.ProcessedAt);
        });
    }
}
