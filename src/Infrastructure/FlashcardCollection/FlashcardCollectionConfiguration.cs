using Domain.FlashcardCollection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.LanguageAccount;

namespace Infrastructure.FlashcardCollection;

internal sealed class FlashcardCollectionConfiguration : IEntityTypeConfiguration<Domain.FlashcardCollection.FlashcardCollection>
{
    public void Configure(EntityTypeBuilder<Domain.FlashcardCollection.FlashcardCollection> builder)
    {
        builder.HasKey(fc => fc.Id);

        builder.Property(fc => fc.Name)
            .IsRequired()
            .HasMaxLength(200);
       
        builder.HasOne<Domain.LanguageAccount.LanguageAccount>()
            .WithMany()
            .HasForeignKey(fc => fc.LanguageAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(fc => fc.Flashcards)
            .WithOne(f => f.FlashcardCollection)
            .HasForeignKey(f => f.FlashcardCollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(fc => fc.Flashcards)
            .HasField("_flashcards")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
