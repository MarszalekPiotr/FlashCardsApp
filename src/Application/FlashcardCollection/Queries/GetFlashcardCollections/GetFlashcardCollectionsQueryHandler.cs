using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Authorization.FlashcardCollection;
using Application.FlashcardCollection.Queries;
using Domain.LanguageAccount;
using Domain.Users;
using SharedKernel;

namespace Application.FlashcardCollection.Queries.GetFlashcardCollections;

internal sealed class GetFlashcardCollectionsQueryHandler(
    IFlashcardCollectionReadRepository readRepository,
    IUserContext userContext,
    CanAccessFlashcardCollectionSpecification canAccessFlashcardCollectionSpecification)
    : IQueryHandler<GetFlashcardCollectionsQuery, List<FlashcardCollectionResponse>>
{
    public async Task<Result<List<FlashcardCollectionResponse>>> Handle(
        GetFlashcardCollectionsQuery query,
        CancellationToken cancellationToken)
    {
        

        List<FlashcardCollectionListReadModel> collections =
            await readRepository.GetByLanguageAccountIdAsync(query.LanguageAccountId);

        bool forbidden = false;
        collections.ForEach(async collection => 
        {
            bool canAccess = await canAccessFlashcardCollectionSpecification.IsSatisfiedByAsync(collection.Id, userContext.UserId, cancellationToken);
            if (!canAccess)
            {
                forbidden = true;
            }
        }
        );

        if (forbidden)
        {
            return Result.Failure<List<FlashcardCollectionResponse>>(AuthorizationError.Forbidden());
        }

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
