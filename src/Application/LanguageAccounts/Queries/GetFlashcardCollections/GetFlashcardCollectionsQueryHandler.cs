using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.LanguageAccounts;
using Application.LanguageAccounts.DTO;
using Domain.LanguageAccount;
using Domain.Users;
using SharedKernel;

namespace Application.LanguageAccounts.Queries.GetFlashcardCollections;

internal sealed class GetFlashcardCollectionsQueryHandler(
    IFlashcardCollectionReadRepository readRepository,
    IUserContext userContext)
    : IQueryHandler<GetFlashcardCollectionsQuery, List<FlashcardCollectionResponse>>
{
    public async Task<Result<List<FlashcardCollectionResponse>>> Handle(
        GetFlashcardCollectionsQuery query,
        CancellationToken cancellationToken)
    {
        Guid? ownerUserId = await readRepository.GetLanguageAccountUserIdAsync(query.LanguageAccountId);

        if (ownerUserId is null)
        {
            return Result.Failure<List<FlashcardCollectionResponse>>(
                LanguageAccountErrors.NotFound(query.LanguageAccountId));
        }

        if (ownerUserId != userContext.UserId)
        {
            return Result.Failure<List<FlashcardCollectionResponse>>(UserErrors.Unauthorized());
        }

        List<FlashcardCollectionListReadModel> collections =
            await readRepository.GetByLanguageAccountIdAsync(query.LanguageAccountId);

        var response = collections
            .Select(c => new FlashcardCollectionResponse
            {
                Id = c.Id,
                LanguageAccountId = c.LanguageAccountId,
                Name = c.Name
            })
            .ToList();

        return response;
    }
}
