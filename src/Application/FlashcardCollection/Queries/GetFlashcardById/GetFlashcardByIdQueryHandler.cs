using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Authorization.FlashcardCollection;
using Application.FlashcardCollection.Queries;
using Domain.FlashcardCollection;
using Domain.Users;
using SharedKernel;

namespace Application.FlashcardCollection.Queries.GetFlashcardById;

internal sealed class GetFlashcardByIdQueryHandler(
    IFlashcardCollectionReadRepository readRepository,
    IUserContext userContext,
    CanAccessFlashcardCollectionSpecification canAccessFlashcardCollectionSpecification)
    : IQueryHandler<GetFlashcardByIdQuery, FlashcardDetailResponse>
{
    public async Task<Result<FlashcardDetailResponse>> Handle(
        GetFlashcardByIdQuery query,
        CancellationToken cancellationToken)
    {
        FlashcardDetailReadModel? flashcard =
            await readRepository.GetFlashcardByIdAsync(query.FlashcardId);

        if (flashcard is null)
        {
            return Result.Failure<FlashcardDetailResponse>(FlashcardErrors.NotFound(query.FlashcardId));
        }

        bool canAccess = await canAccessFlashcardCollectionSpecification.IsSatisfiedByAsync(
            flashcard.FlashcardCollectionId,
            userContext.UserId,
            cancellationToken);

        if (!canAccess)
        {
           return Result.Failure<FlashcardDetailResponse>(AuthorizationError.Forbidden());
        }

        var response = new FlashcardDetailResponse
        {
            Id = flashcard.Id,
            FlashcardCollectionId = flashcard.FlashcardCollectionId,
            SentenceWithBlanks = flashcard.SentenceWithBlanks,
            Translation = flashcard.Translation,
            Answer = flashcard.Answer,
            Synonyms = flashcard.Synonyms
        };

        return response;
    }
}
