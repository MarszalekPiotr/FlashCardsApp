using Application.Abstractions.Data;
using Application.Abstractions.Repository;
using Application.LanguageAccounts;
using Domain.LanguageAccount;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.LanguageAccount;

public class FlashcardCollectionRepository : BaseWriteRepository, IFlashcardCollectionRepository
{
    public FlashcardCollectionRepository(IApplicationDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public async Task<FlashcardCollection?> GetByIdWithLanguageAccountAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _applicationDbContext.FlashcardCollections
            .Include(fc => fc.LanguageAccount)
            .SingleOrDefaultAsync(fc => fc.Id == id, cancellationToken);
    }

    public void Remove(FlashcardCollection collection)
    {
        _applicationDbContext.FlashcardCollections.Remove(collection);
    }
}
