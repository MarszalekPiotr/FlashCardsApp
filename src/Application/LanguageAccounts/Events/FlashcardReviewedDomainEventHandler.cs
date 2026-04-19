using Application.Abstractions.Data;
using SharedKernel;
using Domain.FlashcardCollection;
using Domain.FlashcardCollection.Enums;
using Domain.FlashcardCollection.Events;
using Application.FlashcardCollection;
using Domain.FlashcardCollection.DomainServices;

namespace Application.LanguageAccounts.Events;

internal sealed class FlashcardReviewedDomainEventHandler(
    IFlashcardCollectionRepository flashcardCollectionWriteRepository,
    IApplicationDbContext applicationDbContext,
    SrsCalculationService srsCalculationService,
    IDateTimeProvider dateTimeProvider)
    : IDomainEventHandler<FlashcardReviewedDomainEvent>
{
    public async Task Handle(FlashcardReviewedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var collection  = await flashcardCollectionWriteRepository.GetByIdWithSingleFlashcardAsync(domainEvent.FlashcardCollectionId,domainEvent.FlashcardId,cancellationToken);

        if (collection is null)
        {
            // Log warning or throw exception
            return;
        }

        var flashcard = collection.Flashcards.FirstOrDefault(f => f.Id == domainEvent.FlashcardId); 

        if (flashcard is null)
        {
            // Log warning or throw exception
            return;
        }

        var srsState = flashcard.SrsState;


        var reviewResult = (ReviewResult)domainEvent.ReviewResult;

        /// dodac serwi domenowy do obliczania nowego stanu SRS na podstawie wyniku recenzji
        SrsStateCalculation newSrsState = srsCalculationService.CalculateNextState(srsState, reviewResult,dateTimeProvider.UtcNow );

        flashcard.UpdateSrsState(newSrsState);

        await applicationDbContext.SaveChangesAsync(cancellationToken);
    }
}
