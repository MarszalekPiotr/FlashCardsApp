using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.LanguageAccounts;
using Application.LanguageAccounts.DTO;
using Domain.LanguageAccount;
using Domain.Users;
using SharedKernel;

namespace Application.LanguageAccounts.Queries.GetFlashcardCollectionById;

internal sealed class GetFlashcardCollectionByIdQueryHandler(
    IFlashcardCollectionReadRepository readRepository,
    IUserContext userContext)
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

        if (collection.UserId != userContext.UserId)
        {
            return Result.Failure<FlashcardCollectionDetailResponse>(UserErrors.Unauthorized());
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
