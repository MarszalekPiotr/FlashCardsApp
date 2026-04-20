using System.Text.Json;
using Domain.FlashcardCollection;
using Domain.LanguageAccount.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.FlashcardCollection;

internal class FlashcardConfiguration : IEntityTypeConfiguration<Flashcard>
{
    public void Configure(EntityTypeBuilder<Flashcard> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.SentenceWithBlanks)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.Translation)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.Answer)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.Synonyms)
            .HasConversion(
                synonyms => JsonSerializer.Serialize(synonyms.Value, (JsonSerializerOptions?)null),
                value => new Synonyms(JsonSerializer.Deserialize<List<string>>(value, (JsonSerializerOptions?)null)!))
            .HasColumnType("nvarchar(max)");

        builder.HasOne(f => f.FlashcardCollection)
            .WithMany(fc => fc.Flashcards)
            .HasForeignKey(f => f.FlashcardCollectionId)
            .OnDelete(DeleteBehavior.Cascade);

       
    }
}
