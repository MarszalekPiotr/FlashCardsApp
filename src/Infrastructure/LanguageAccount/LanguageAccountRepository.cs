using Application.Abstractions.Data;
using Application.Abstractions.Repository;
using Application.LanguageAccounts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.LanguageAccount;

public class LanguageAccountRepository : BaseWriteRepository, ILanguageAccountRepository
{
    public LanguageAccountRepository(IApplicationDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public async Task<Domain.LanguageAccount.LanguageAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _applicationDbContext.LanguageAccounts
            .SingleOrDefaultAsync(la => la.Id == id, cancellationToken);
    }

    public async Task AddAsync(Domain.LanguageAccount.LanguageAccount account)
    {
        await _applicationDbContext.LanguageAccounts.AddAsync(account);
    }

    public void Remove(Domain.LanguageAccount.LanguageAccount account)
    {
        _applicationDbContext.LanguageAccounts.Remove(account);
    }
}
