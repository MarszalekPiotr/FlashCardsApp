using Domain.LanguageAccount;

namespace Application.LanguageAccounts;

public interface IFlashcardRepository
{
    Task<Flashcard?> GetByIdWithCollectionAsync(Guid id, CancellationToken cancellationToken);

    void Remove(Flashcard flashcard);
}
