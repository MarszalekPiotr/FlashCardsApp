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
            new Language { Id = Guid.NewGuid(), Name = "English", Code = "EN", IsActive = true },
            new Language { Id = Guid.NewGuid(), Name = "Spanish", Code = "ES", IsActive = true },
            new Language { Id = Guid.NewGuid(), Name = "French", Code = "FR", IsActive = true },
            new Language { Id = Guid.NewGuid(), Name = "German", Code = "DE", IsActive = true },
            new Language { Id = Guid.NewGuid(), Name = "Chinese", Code = "ZH", IsActive = true },
            new Language { Id = Guid.NewGuid(), Name = "Japanese", Code = "JA", IsActive = true },
            new Language { Id = Guid.NewGuid(), Name = "Russian", Code = "RU", IsActive = true },
            new Language { Id = Guid.NewGuid(), Name = "Polish", Code = "PL", IsActive = true }
            );
    }
}
