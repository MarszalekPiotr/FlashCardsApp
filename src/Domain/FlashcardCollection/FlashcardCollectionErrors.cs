using SharedKernel;

namespace Domain.FlashcardCollection;

public static class FlashcardCollectionErrors
{
    public static Error NotFound(Guid flashcardCollectionId) => Error.NotFound(
        "FlashcardCollections.NotFound",
        $"The flashcard collection with Id = '{flashcardCollectionId}' was not found.");
}
