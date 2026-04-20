using System.Text.Json;
using Application;
using Application.Abstractions.Data;
using Domain.FlashcardCollection;
using Domain.Todos;
using Domain.Users;
using Infrastructure.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SharedKernel;

namespace Infrastructure.Database;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDateTimeProvider dateTimeProvider)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<TodoItem> TodoItems { get; set; }

    public DbSet<Domain.LanguageAccount.LanguageAccount> LanguageAccounts { get; set; }

    public DbSet<Domain.FlashcardCollection.FlashcardCollection> FlashcardCollections { get; set; }

    public DbSet<Flashcard> Flashcards { get; set; }

    public DbSet<SrsState> SrsStates { get; set; }

    public DbSet<FlashcardReview> FlashcardReviews { get; set; }

    public DbSet<OutboxMessage> OutboxMessages {  get; set; }

    public DbSet<OutboxMessageConsumer> OutboxMessageConsumers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.HasDefaultSchema(Schemas.Default);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // When should you publish domain events?
        //
        // 1. BEFORE calling SaveChangesAsync
        //     - domain events are part of the same transaction
        //     - immediate consistency
        // 2. AFTER calling SaveChangesAsync
        //     - domain events are a separate transaction
        //     - eventual consistency
        //     - handlers can fail


        await PublishDomainEventsAsync(cancellationToken);
        int result = await base.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Database.CurrentTransaction is not null)
        {
            await Database.CurrentTransaction.CommitAsync(cancellationToken);
        }

    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Database.CurrentTransaction is not null)
        {
            await Database.CurrentTransaction.RollbackAsync(cancellationToken);
        }
    }

    private async Task PublishDomainEventsAsync(CancellationToken cancellationToken = default)
    {
        //var domainEvents = ChangeTracker
        //    .Entries<Entity>()
        //    .Select(entry => entry.Entity)
        //    .SelectMany(entity =>
        //    {
        //        List<IDomainEvent> domainEvents = entity.DomainEvents;

        //        entity.ClearDomainEvents();

        //        return domainEvents;
        //    })
        //    .ToList();

        //await domainEventsDispatcher.DispatchAsync(domainEvents);

        var outboxMessages = ChangeTracker
       .Entries<Entity>()
       .SelectMany(e => e.Entity.DomainEvents)
       .Select(domainEvent => new OutboxMessage
       {
           Id = Guid.NewGuid(),
           Type = $"{domainEvent.GetType().FullName}, {domainEvent.GetType().Assembly.GetName().Name}",
           Content = JsonSerializer.Serialize(domainEvent),
           OccurredOnUtc = dateTimeProvider.UtcNow
       })
       .ToList();

        await OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);
        ChangeTracker.Entries<Entity>().ToList().ForEach(e => e.Entity.ClearDomainEvents());


    }
}
