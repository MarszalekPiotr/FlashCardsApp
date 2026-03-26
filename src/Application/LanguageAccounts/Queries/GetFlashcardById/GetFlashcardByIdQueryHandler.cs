using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.LanguageAccounts.DTO;
using Domain.LanguageAccount;
using Domain.Users;
using SharedKernel;

namespace Application.LanguageAccounts.Queries.GetFlashcardById;

internal sealed class GetFlashcardByIdQueryHandler(
    IFlashcardCollectionReadRepository readRepository,
    IUserContext userContext)
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

        if (flashcard.UserId != userContext.UserId)
        {
            return Result.Failure<FlashcardDetailResponse>(UserErrors.Unauthorized());
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
