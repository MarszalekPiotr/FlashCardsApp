using Application.Abstractions.Data;
using Application.Abstractions.Repository;
using Application.FlashcardCollection;
using Domain.FlashcardCollection;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.FlashcardCollection;

internal sealed class FlashcardRepository : BaseWriteRepository, IFlashcardRepository
{
    public FlashcardRepository(IApplicationDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public async Task<Flashcard?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _applicationDbContext.Flashcards
            .Include(f => f.SrsState)
            .SingleOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task AddAsync(Flashcard flashcard)
    {
        await _applicationDbContext.Flashcards.AddAsync(flashcard);
    }

    public void Remove(Flashcard flashcard)
    {
        _applicationDbContext.Flashcards.Remove(flashcard);
    }
}
