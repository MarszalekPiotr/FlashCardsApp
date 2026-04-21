using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Authorization.FlashcardCollection;
using Application.FlashcardCollection.Queries;
using Domain.FlashcardCollection;
using Domain.Users;
using SharedKernel;

namespace Application.FlashcardCollection.Queries.GetFlashcardCollectionById;

internal sealed class GetFlashcardCollectionByIdQueryHandler(
    IFlashcardCollectionReadRepository readRepository,
    IUserContext userContext,
    CanAccessFlashcardCollectionSpecification canAccessFlashcardCollectionSpecification)
    : IQueryHandler<GetFlashcardCollectionByIdQuery, FlashcardCollectionDetailResponse>
{
    public async Task<Result<FlashcardCollectionDetailResponse>> Handle(
        GetFlashcardCollectionByIdQuery query,
        CancellationToken cancellationToken)
    {
        FlashcardCollectionDetailReadModel? collection =
            await readRepository.GetByIdAsync(query.FlashcardCollectionId);

        if (collection is null)
        {
            return Result.Failure<FlashcardCollectionDetailResponse>(
                FlashcardCollectionErrors.NotFound(query.FlashcardCollectionId));
        }

       bool canAccess = await canAccessFlashcardCollectionSpecification.IsSatisfiedByAsync(
            collection.Id, userContext.UserId, cancellationToken);

        if (!canAccess)
        {
            return Result.Failure<FlashcardCollectionDetailResponse>(
                AuthorizationError.Forbidden());
        }

            var response = new FlashcardCollectionDetailResponse
        {
            Id = collection.Id,
            LanguageAccountId = collection.LanguageAccountId,
            Name = collection.Name,
            Flashcards = collection.Flashcards.Select(f => new FlashcardResponse
            {
                Id = f.Id,
                SentenceWithBlanks = f.SentenceWithBlanks,
                Translation = f.Translation,
                Answer = f.Answer,
                Synonyms = f.Synonyms
            }).ToList()
        };

        return response;
    }
}
