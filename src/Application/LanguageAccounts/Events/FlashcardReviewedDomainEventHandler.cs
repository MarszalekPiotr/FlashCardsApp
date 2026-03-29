using Application.Abstractions.Data;
using Application.SRS;
using Domain.SRS.Events;
using Domain.SRS;
using SharedKernel;

namespace Application.LanguageAccounts.Events;

internal sealed class FlashcardReviewedDomainEventHandler(
    ISrsStateRepository srsStateRepository,
    IUnitOfWork unitOfWork)
    : IDomainEventHandler<FlashcardReviewedDomainEvent>
{
    public async Task Handle(FlashcardReviewedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        SrsState? srsState = await srsStateRepository.GetByFlashcardIdAsync(domainEvent.FlashcardId, cancellationToken);

        if (srsState is null)
        {
            srsState = SrsState.CreateInitialState(domainEvent.FlashcardId);
            srsStateRepository.Add(srsState);
        }

        var reviewResult = new Domain.SRS.ValueObjects.ReviewResult(
            (Domain.SRS.Enums.ReviewResult)domainEvent.ReviewResultValue);

        srsState.UpdateState(reviewResult);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
