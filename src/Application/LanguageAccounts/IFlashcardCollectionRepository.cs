using Domain.LanguageAccount;

namespace Application.LanguageAccounts;

public interface IFlashcardCollectionRepository
{
    Task<FlashcardCollection?> GetByIdWithLanguageAccountAsync(Guid id, CancellationToken cancellationToken);

    void Remove(FlashcardCollection collection);
}
