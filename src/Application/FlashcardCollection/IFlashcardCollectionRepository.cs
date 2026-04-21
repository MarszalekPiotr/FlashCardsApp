using Domain.FlashcardCollection;

namespace Application.FlashcardCollection;

public interface IFlashcardCollectionRepository
{
    Task<Domain.FlashcardCollection.FlashcardCollection?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Domain.FlashcardCollection.FlashcardCollection collection);

    void Remove(Domain.FlashcardCollection.FlashcardCollection collection);
}
