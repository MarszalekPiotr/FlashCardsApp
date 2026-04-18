using System.Text.Json;
using Domain.LanguageAccount;
using Domain.LanguageAccount.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.SharedEntities.Language;

namespace Infrastructure.LanguageAccount;

public class LanguageAccountConfiguration : IEntityTypeConfiguration<Domain.LanguageAccount.LanguageAccount>
{
    public void Configure(EntityTypeBuilder<Domain.LanguageAccount.LanguageAccount> builder)
    {
        builder.HasKey(la => la.Id);

        builder.HasOne<Language>()
            .WithMany()
            .HasForeignKey(la => la.LanguageId)
            .OnDelete(DeleteBehavior.Cascade);


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
