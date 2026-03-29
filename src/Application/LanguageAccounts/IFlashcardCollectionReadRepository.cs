using Application.LanguageAccounts.DTO;

namespace Application.LanguageAccounts;

public interface IFlashcardCollectionReadRepository
{
    Task<Guid?> GetLanguageAccountUserIdAsync(Guid languageAccountId);

    Task<List<FlashcardCollectionListReadModel>> GetByLanguageAccountIdAsync(Guid languageAccountId);

    Task<FlashcardCollectionDetailReadModel?> GetByIdAsync(Guid id);

    Task<FlashcardDetailReadModel?> GetFlashcardByIdAsync(Guid flashcardId);

    Task<List<DueFlashcardReadModel>> GetDueFlashcardsAsync(Guid collectionId, Guid userId);
}
