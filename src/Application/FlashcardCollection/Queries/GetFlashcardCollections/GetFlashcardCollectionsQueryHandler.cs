using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Authorization.LanguageAccount;
using Application.FlashcardCollection.Queries;
using Domain.LanguageAccount;
using Domain.Users;
using SharedKernel;

namespace Application.FlashcardCollection.Queries.GetFlashcardCollections;

internal sealed class GetFlashcardCollectionsQueryHandler(
    IFlashcardCollectionReadRepository readRepository,
    IUserContext userContext,
    CanAccessLanguageAccountSpecification canAccessLanguageAccountSpecification)
    : IQueryHandler<GetFlashcardCollectionsQuery, List<FlashcardCollectionResponse>>
{
    public async Task<Result<List<FlashcardCollectionResponse>>> Handle(
        GetFlashcardCollectionsQuery query,
        CancellationToken cancellationToken)
    {
        bool canAccess = await canAccessLanguageAccountSpecification.IsSatisfiedByAsync(
            query.LanguageAccountId,
            userContext.UserId,
            cancellationToken);

        if (!canAccess)
        {
            return Result.Failure<List<FlashcardCollectionResponse>>(AuthorizationError.Forbidden());
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
