using Application.Abstractions.Data;
using Application.Abstractions.Repository;
using Application.FlashcardCollection;
using Domain.FlashcardCollection;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.LanguageAccount;

public class FlashcardCollectionRepository : BaseWriteRepository, IFlashcardCollectionRepository
{
    public FlashcardCollectionRepository(IApplicationDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public void Add(FlashcardCollection collection)
    {
        _applicationDbContext.FlashcardCollections.Add(collection);
    }

    public async Task<FlashcardCollection?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _applicationDbContext.FlashcardCollections
            .SingleOrDefaultAsync(fc => fc.Id == id, cancellationToken);
    }

    public async Task<FlashcardCollection?> GetByIdWithSingleFlashcardAsync(Guid id, Guid flashcardId, CancellationToken cancellationToken)
    {
        return await _applicationDbContext.FlashcardCollections
            .Include(fc => fc.Flashcards.Where(f => f.Id == flashcardId)).ThenInclude(f => f.SrsState)
            .SingleOrDefaultAsync(fc => fc.Id == id, cancellationToken);
    }

    public void Remove(FlashcardCollection collection)
    {
        _applicationDbContext.FlashcardCollections.Remove(collection);
    }
}
