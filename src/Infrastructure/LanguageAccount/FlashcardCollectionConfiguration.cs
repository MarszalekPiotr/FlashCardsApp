using Domain.LanguageAccount;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.LanguageAccount;

internal sealed class FlashcardCollectionConfiguration : IEntityTypeConfiguration<FlashcardCollection>
{
    public void Configure(EntityTypeBuilder<FlashcardCollection> builder)
    {
        builder.HasKey(fc => fc.Id);

        builder.Property(fc => fc.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(fc => fc.Flashcards)
            .WithOne(f => f.FlashcardCollection)
            .HasForeignKey(f => f.FlashcardCollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(fc => fc.Flashcards)
            .HasField("_flashcards")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
