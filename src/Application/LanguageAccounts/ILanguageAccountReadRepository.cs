using Application.LanguageAccounts.DTO;

namespace Application.LanguageAccounts;

public interface ILanguageAccountReadRepository
{
    Task<List<LanguageAccountListReadModel>> GetByUserIdAsync(Guid userId);

    Task<LanguageAccountDetailReadModel?> GetByIdAsync(Guid id);
}
