using Domain.FlashcardCollection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.FlashcardCollection;

internal sealed class SrsStateConfiguration : IEntityTypeConfiguration<SrsState>
{
    public void Configure(EntityTypeBuilder<SrsState> builder)
    {
       

        builder.Property(s => s.Interval).IsRequired();
        builder.Property(s => s.EaseFactor).IsRequired();
        builder.Property(s => s.Repetitions).IsRequired();
        builder.Property(s => s.NextReviewDate).IsRequired();
        builder.HasKey(s => s.FlashcardId); // shared PK = FK pattern

        builder.HasOne<Flashcard>()
            .WithOne(f => f.SrsState)
            .HasForeignKey<SrsState>(s => s.FlashcardId) // FK is on SrsState
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("SrsStates");
    }
}
