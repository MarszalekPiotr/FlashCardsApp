using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.SharedEntities.Language;

namespace Infrastructure.Shared;

internal class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(l => l.Code)
            .IsRequired()
            .HasMaxLength(5);

        builder.HasData(
            new Language { Id = new Guid("11111111-1111-1111-1111-111111111111"), Name = "English", Code = "EN", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Language { Id = new Guid("22222222-2222-2222-2222-222222222222"), Name = "Spanish", Code = "ES", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Language { Id = new Guid("33333333-3333-3333-3333-333333333333"), Name = "French", Code = "FR", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Language { Id = new Guid("44444444-4444-4444-4444-444444444444"), Name = "German", Code = "DE", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Language { Id = new Guid("55555555-5555-5555-5555-555555555555"), Name = "Chinese", Code = "ZH", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Language { Id = new Guid("66666666-6666-6666-6666-666666666666"), Name = "Japanese", Code = "JA", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Language { Id = new Guid("77777777-7777-7777-7777-777777777777"), Name = "Russian", Code = "RU", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Language { Id = new Guid("88888888-8888-8888-8888-888888888888"), Name = "Polish", Code = "PL", IsActive = true, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
    }
}
