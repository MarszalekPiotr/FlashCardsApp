using Domain.FlashcardCollection.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.FlashcardCollection;

namespace Infrastructure.FlashcardCollection;

internal sealed class FlashcardReviewConfiguration : IEntityTypeConfiguration<FlashcardReview>
{
    public void Configure(EntityTypeBuilder<FlashcardReview> builder)
    {
        builder.HasKey(fr => fr.Id);

        builder.Property(fr => fr.ReviewDate).IsRequired();

        builder.Property(fr => fr.FlashcardId).IsRequired();

        // Configure the relationship explicitly to avoid shadow property FlashcardId1
        builder.HasOne<Flashcard>()
            .WithMany(f => f.Reviews)
            .HasForeignKey(fr => fr.FlashcardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
