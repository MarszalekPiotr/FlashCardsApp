using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.LanguageAccounts.DTO;
using SharedKernel;

namespace Application.LanguageAccounts.Queries.GetDueFlashcards;

internal sealed class GetDueFlashcardsQueryHandler(
    IFlashcardCollectionReadRepository readRepository,
    IUserContext userContext)
    : IQueryHandler<GetDueFlashcardsQuery, List<DueFlashcardResponse>>
{
    public async Task<Result<List<DueFlashcardResponse>>> Handle(
        GetDueFlashcardsQuery query,
        CancellationToken cancellationToken)
    {
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
