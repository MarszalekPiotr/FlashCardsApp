using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Authorization.FlashcardCollection;
using Application.FlashcardCollection.Queries;
using SharedKernel;

namespace Application.FlashcardCollection.Queries.GetDueFlashcards;

internal sealed class GetDueFlashcardsQueryHandler(
    IFlashcardCollectionReadRepository readRepository,
    IUserContext userContext,
    CanAccessFlashcardCollectionSpecification canAccessFlashcardCollectionSpecification)
    : IQueryHandler<GetDueFlashcardsQuery, List<DueFlashcardResponse>>
{
    public async Task<Result<List<DueFlashcardResponse>>> Handle(
        GetDueFlashcardsQuery query,
        CancellationToken cancellationToken)
    {    

        bool canaccess = await canAccessFlashcardCollectionSpecification.IsSatisfiedByAsync(
            query.CollectionId, userContext.UserId, cancellationToken);

        if (!canaccess)
        {
            return Result.Failure<List<DueFlashcardResponse>>(AuthorizationError.Forbidden());
        }

        List<DueFlashcardReadModel> flashcards =
            await readRepository.GetDueFlashcardsAsync(query.CollectionId, userContext.UserId);

        List<DueFlashcardResponse> response = flashcards
            .Select(f => new DueFlashcardResponse
            {
                Id = f.Id,
                SentenceWithBlanks = f.SentenceWithBlanks,
                Translation = f.Translation,
                Answer = f.Answer,
                Synonyms = f.Synonyms,
                NextReviewDate = f.NextReviewDate
            })
            .ToList();

        return response;
    }
}
