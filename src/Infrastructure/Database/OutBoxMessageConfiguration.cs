using System;
using System.Collections.Generic;
using System.Text;
using Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(o => o.Content)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(o => o.Error)
            .HasMaxLength(2000);

        builder.HasIndex(o => new { o.ProcessedOnUtc, o.RetryCount });
    }
}
