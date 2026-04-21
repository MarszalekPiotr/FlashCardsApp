using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Authorization.FlashcardCollection;
using Domain.FlashcardCollection;
using Domain.FlashcardCollection.DomainServices;
using Domain.FlashcardCollection.Enums;
using SharedKernel;

namespace Application.FlashcardCollection.Commands.AddFlashcardReview;

internal sealed class AddFlashcardReviewCommandHandler(
    IFlashcardRepository flashcardRepository,
    IApplicationDbContext applicationDbContext,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext,
    SrsCalculationService srsCalculationService,
    CanAccessFlashcardCollectionSpecification canAccessFlashcardCollectionSpecification)
    : ICommandHandler<AddFlashcardReviewCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddFlashcardReviewCommand command, CancellationToken cancellationToken)
    {
        Flashcard? flashcard = await flashcardRepository.GetByIdAsync(command.FlashcardId, cancellationToken);

        if (flashcard is null)
        {
            return Result.Failure<Guid>(FlashcardErrors.NotFound(command.FlashcardId));
        }

        bool canAccess = await canAccessFlashcardCollectionSpecification.IsSatisfiedByAsync(flashcard.FlashcardCollectionId, userContext.UserId, cancellationToken);

        if (!canAccess)
        {
            return Result.Failure<Guid>(AuthorizationError.Forbidden());
        }

        var reviewResult = (ReviewResult)command.ReviewResult;
        FlashcardReview review = flashcard.AddReview(reviewResult, srsCalculationService, dateTimeProvider.UtcNow);

        await applicationDbContext.SaveChangesAsync(cancellationToken);

        return review.Id;
    }
}
