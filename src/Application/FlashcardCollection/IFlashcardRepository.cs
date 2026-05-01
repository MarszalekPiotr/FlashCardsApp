using Domain.FlashcardCollection;

namespace Application.FlashcardCollection;

public interface IFlashcardRepository
{
    Task<Flashcard?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Flashcard flashcard);

    void Remove(Flashcard flashcard);
}
