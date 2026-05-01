using Domain.FlashcardCollection;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;


namespace Application.Abstractions.Data;

/// <summary>
/// Database abstraction exposed to the Application layer.
/// Only aggregate roots are listed here — child entities (FlashcardReview, SrsState)
/// must be accessed through their aggregate root, not queried directly.
/// Infrastructure concerns (OutboxMessage, OutboxMessageConsumer) are handled
/// entirely within the Infrastructure layer via the concrete ApplicationDbContext.
/// </summary>
public interface IApplicationDbContext
{
    // Aggregate roots only
    DbSet<User> Users { get; }
    DbSet<Domain.LanguageAccount.LanguageAccount> LanguageAccounts { get; }
    DbSet<Domain.FlashcardCollection.FlashcardCollection> FlashcardCollections { get; }
    DbSet<Flashcard> Flashcards { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
