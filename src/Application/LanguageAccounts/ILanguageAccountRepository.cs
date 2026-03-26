using Domain.LanguageAccount;

namespace Application.LanguageAccounts;

public interface ILanguageAccountRepository
{
    Task<Domain.LanguageAccount.LanguageAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Domain.LanguageAccount.LanguageAccount account);

    void Remove(Domain.LanguageAccount.LanguageAccount account);
}
