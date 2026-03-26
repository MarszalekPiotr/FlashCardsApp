using System.Text.Json;
using Domain.LanguageAccount;
using Domain.LanguageAccount.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.LanguageAccount;

public class LanguageAccountConfiguration : IEntityTypeConfiguration<Domain.LanguageAccount.LanguageAccount>
{
    public void Configure(EntityTypeBuilder<Domain.LanguageAccount.LanguageAccount> builder)
    {
        builder.HasKey(la => la.Id);

        builder.Property(la => la.Language)
            .HasConversion(
                language => JsonSerializer.Serialize(language, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Language>(json, (JsonSerializerOptions?)null)!)
            .HasColumnType("nvarchar(100)");

        builder.Property(la => la.ProficiencyLevel)
            .HasConversion(
                level => level.Value,
                value => new ProficiencyLevel(value))
            .IsRequired();

        builder.HasMany(la => la.FlashcardCollections)
               .WithOne(fc => fc.LanguageAccount)
               .HasForeignKey(fc => fc.LanguageAccountId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(la => la.FlashcardCollections)
            .HasField("_flashcardCollections")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
