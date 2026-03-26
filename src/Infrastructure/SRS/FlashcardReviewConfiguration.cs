using Domain.SRS;
using Domain.SRS.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.SRS;

internal sealed class FlashcardReviewConfiguration : IEntityTypeConfiguration<FlashcardReview>
{
    public void Configure(EntityTypeBuilder<FlashcardReview> builder)
    {
        builder.HasKey(fr => fr.Id);

        builder.Property(fr => fr.ReviewResult)
            .HasConversion(
                result => result.Value,
                value => new ReviewResult(value))
            .IsRequired();

        builder.Property(fr => fr.ReviewDate)
            .IsRequired();

        builder.Property(fr => fr.FlashcardId)
            .IsRequired();
    }
}
