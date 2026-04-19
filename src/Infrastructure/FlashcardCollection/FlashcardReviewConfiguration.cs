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
    }
}
