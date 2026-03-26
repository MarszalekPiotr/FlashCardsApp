using SharedKernel;

namespace Domain.LanguageAccount;

public static class FlashcardErrors
{
    public static Error NotFound(Guid flashcardId) => Error.NotFound(
        "Flashcards.NotFound",
        $"The flashcard with Id = '{flashcardId}' was not found.");
}
