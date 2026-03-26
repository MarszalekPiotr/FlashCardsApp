using Application.Abstractions.Data;
using Application.Abstractions.Repository;
using Application.LanguageAccounts;
using Domain.LanguageAccount;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.LanguageAccount;

public class FlashcardRepository : BaseWriteRepository, IFlashcardRepository
{
    public FlashcardRepository(IApplicationDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public async Task<Flashcard?> GetByIdWithCollectionAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _applicationDbContext.Flashcards
            .Include(f => f.FlashcardCollection)
            .ThenInclude(fc => fc!.LanguageAccount)
            .SingleOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public void Remove(Flashcard flashcard)
    {
        _applicationDbContext.Flashcards.Remove(flashcard);
    }
}
