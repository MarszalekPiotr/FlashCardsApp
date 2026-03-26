using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.LanguageAccounts.DTO;
using SharedKernel;

namespace Application.LanguageAccounts.Queries.GetLanguageAccounts;

internal sealed class GetLanguageAccountsQueryHandler(
    ILanguageAccountReadRepository readRepository,
    IUserContext userContext)
    : IQueryHandler<GetLanguageAccountsQuery, List<LanguageAccountResponse>>
{
    public async Task<Result<List<LanguageAccountResponse>>> Handle(
        GetLanguageAccountsQuery query,
        CancellationToken cancellationToken)
    {
        List<LanguageAccountListReadModel> accounts =
            await readRepository.GetByUserIdAsync(userContext.UserId);

        var response = accounts
            .Select(a => new LanguageAccountResponse
            {
                Id = a.Id,
                LanguageCode = a.LanguageCode,
                LanguageFullName = a.LanguageFullName,
                ProficiencyLevel = a.ProficiencyLevel
            })
            .ToList();

        return response;
    }
}
