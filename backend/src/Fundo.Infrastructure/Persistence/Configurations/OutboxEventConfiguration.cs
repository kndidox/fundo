using Fundo.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fundo.Infrastructure.Persistence.Configurations;

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("outbox_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Ssn).IsRequired().HasMaxLength(9);
        builder.Property(e => e.PayloadJson).IsRequired();
        builder.Property(e => e.LastError).HasMaxLength(2000);
        builder.Property(e => e.EventType).HasConversion<string>().HasMaxLength(40);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        // The dispatcher polls by this exact filter.
        builder.HasIndex(e => new { e.Status, e.NextAttemptAt });
    }
}
