using Domain.FlashcardCollection;
using Domain.Todos;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;


namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<TodoItem> TodoItems { get; }
    DbSet<Domain.LanguageAccount.LanguageAccount> LanguageAccounts { get; }
    DbSet<Domain.FlashcardCollection.FlashcardCollection> FlashcardCollections { get; }
    DbSet<Flashcard> Flashcards { get; }
    DbSet<FlashcardReview> FlashcardReviews { get; }
    DbSet<SrsState> SrsStates { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<OutboxMessageConsumer> OutboxMessageConsumers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
