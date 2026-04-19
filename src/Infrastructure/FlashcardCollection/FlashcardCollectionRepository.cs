using Application.Abstractions.Data;
using Application.Abstractions.Repository;
using Application.FlashcardCollection;
using Domain.FlashcardCollection;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.FlashcardCollection;

public class FlashcardCollectionRepository : BaseWriteRepository, IFlashcardCollectionRepository
{
    public FlashcardCollectionRepository(IApplicationDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public async Task AddAsync(Domain.FlashcardCollection.FlashcardCollection collection)
    {
        await _applicationDbContext.FlashcardCollections.AddAsync(collection);
    }

    public async Task<Domain.FlashcardCollection.FlashcardCollection?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _applicationDbContext.FlashcardCollections
            .SingleOrDefaultAsync(fc => fc.Id == id, cancellationToken);
    }

    public async Task<Domain.FlashcardCollection.FlashcardCollection?> GetByIdWithSingleFlashcardAsync(Guid id, Guid flashcardId, CancellationToken cancellationToken)
    {
        return await _applicationDbContext.FlashcardCollections
            .Include(fc => fc.Flashcards.Where(f => f.Id == flashcardId)).ThenInclude(f => f.SrsState)
            .SingleOrDefaultAsync(fc => fc.Id == id, cancellationToken);
    }

    public void Remove(Domain.FlashcardCollection.FlashcardCollection collection)
    {
        _applicationDbContext.FlashcardCollections.Remove(collection);
    }
}
