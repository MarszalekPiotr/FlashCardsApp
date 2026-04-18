using Domain.FlashcardCollection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.SRS;

internal sealed class SrsStateConfiguration : IEntityTypeConfiguration<SrsState>
{
    public void Configure(EntityTypeBuilder<SrsState> builder)
    {
        builder.HasKey(s => s.FlashcardId);

        builder.Property(s => s.Interval).IsRequired();
        builder.Property(s => s.EaseFactor).IsRequired();
        builder.Property(s => s.Repetitions).IsRequired();
        builder.Property(s => s.NextReviewDate).IsRequired();

        builder.HasOne<Flashcard>()
            .WithOne()
            .HasForeignKey<SrsState>(s => s.FlashcardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("SrsStates");
    }
}
