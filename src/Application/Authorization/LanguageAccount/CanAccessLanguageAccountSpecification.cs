using System;
using System.Collections.Generic;
using System.Text;
using Application.LanguageAccounts;

namespace Application.Authorization.LanguageAccount;

public sealed class CanAccessLanguageAccountSpecification : IAuthorizationSpecification<Guid>
{   
    private readonly ILanguageAccountReadRepository _languageAccountReadRepository;

    public CanAccessLanguageAccountSpecification(ILanguageAccountReadRepository languageAccountReadRepository)
    {
        _languageAccountReadRepository = languageAccountReadRepository;
    }

    public async Task<bool> IsSatisfiedByAsync(Guid languageAccountId, Guid userId, CancellationToken cancellationToken)
    {
        var languageAccount = await _languageAccountReadRepository.GetByIdAsync(languageAccountId);
        if (languageAccount is null)
        {
            return false;
        }

        return languageAccount.UserId == userId;
    }
}
