using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.LanguageAccounts.DTO;
using Domain.LanguageAccount;
using Domain.Users;
using SharedKernel;

namespace Application.LanguageAccounts.Queries.GetLanguageAccountById;

internal sealed class GetLanguageAccountByIdQueryHandler(
    ILanguageAccountReadRepository readRepository,
    IUserContext userContext)
    : IQueryHandler<GetLanguageAccountByIdQuery, LanguageAccountDetailResponse>
{
    public async Task<Result<LanguageAccountDetailResponse>> Handle(
        GetLanguageAccountByIdQuery query,
        CancellationToken cancellationToken)
    {
        LanguageAccountDetailReadModel? account =
            await readRepository.GetByIdAsync(query.LanguageAccountId);

        if (account is null)
        {
            return Result.Failure<LanguageAccountDetailResponse>(
                LanguageAccountErrors.NotFound(query.LanguageAccountId));
        }

        if (account.UserId != userContext.UserId)
        {
            return Result.Failure<LanguageAccountDetailResponse>(UserErrors.Unauthorized());
        }

        var response = new LanguageAccountDetailResponse
        {
            Id = account.Id,
            LanguageCode = account.LanguageCode,
            LanguageFullName = account.LanguageFullName,
            ProficiencyLevel = account.ProficiencyLevel
        };

        return response;
    }
}
