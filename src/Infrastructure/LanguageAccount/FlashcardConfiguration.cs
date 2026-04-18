using System.Text.Json;
using Domain.FlashcardCollection;
using Domain.LanguageAccount.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.LanguageAccount;

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
                synonyms => string.Join("|", synonyms.Value),
                value => new Synonyms(value.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList()))
            .HasMaxLength(1000);

        builder.HasOne(f => f.FlashcardCollection)
            .WithMany(fc => fc.Flashcards)
            .HasForeignKey(f => f.FlashcardCollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.SrsState)
            .WithOne()
            .HasForeignKey<Flashcard>(f => f.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
