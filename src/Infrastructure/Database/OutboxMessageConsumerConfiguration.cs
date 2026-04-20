using Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database;

public sealed class OutboxMessageConsumerConfiguration : IEntityTypeConfiguration<OutboxMessageConsumer>
{
    public void Configure(EntityTypeBuilder<OutboxMessageConsumer> builder)
    {
        builder.HasKey(c => new { c.OutboxMessageId, c.HandlerType });

        builder.Property(c => c.HandlerType)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(c => c.OutboxMessageId);
    }
}
