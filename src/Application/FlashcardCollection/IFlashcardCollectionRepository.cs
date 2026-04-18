using Domain.FlashcardCollection;

namespace Application.FlashcardCollection;

public interface IFlashcardCollectionRepository
{
    Task<Domain.FlashcardCollection.FlashcardCollection?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void  Add(Domain.FlashcardCollection.FlashcardCollection collection);


    Task<Domain.FlashcardCollection.FlashcardCollection?> GetByIdWithSingleFlashcardAsync(Guid id, Guid flashcardId, CancellationToken cancellationToken);

    void Remove(Domain.FlashcardCollection.FlashcardCollection collection);
}
