using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Authorization.FlashcardCollection;
using Domain.FlashcardCollection;
using Domain.FlashcardCollection.Enums;
using Domain.FlashcardCollection.Events;
using Domain.Users;
using SharedKernel;

namespace Application.FlashcardCollection.Commands.AddFlashcardReview;

internal sealed class AddFlashcardReviewCommandHandler(
    IFlashcardReviewRepository flashcardReviewRepository,
    IApplicationDbContext applicationDbContext,
    IDateTimeProvider dateTimeProvider,
    IFlashcardCollectionRepository flashcardCollectionRepository,
    IUserContext userContext,
    CanAccessFlashcardCollectionSpecification canAccessFlashcardCollectionSpecification)
    : ICommandHandler<AddFlashcardReviewCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddFlashcardReviewCommand command, CancellationToken cancellationToken)
    {


        Domain.FlashcardCollection.FlashcardCollection collection = await flashcardCollectionRepository.GetByIdWithSingleFlashcardAsync(command.FlaschardCollectionId, command.FlashcardId, cancellationToken);

        if (collection is null)
        {
            return (Result<Guid>)Result.Failure(FlashcardCollectionErrors.NotFound(command.FlashcardId));
        }

        if (collection.Flashcards == null || !collection.Flashcards.Any())
        {
            return (Result<Guid>)Result.Failure(FlashcardErrors.NotFound(command.FlashcardId));
        }

       bool canAccess = await canAccessFlashcardCollectionSpecification.IsSatisfiedByAsync(collection.Id, userContext.UserId, cancellationToken);  

        if(!canAccess)
        {
            return Result.Failure<Guid>(AuthorizationError.Forbidden());
        }

        var reviewResult = (ReviewResult)command.ReviewResult;
        var review = FlashcardReview.Create(command.FlashcardId, dateTimeProvider.UtcNow, reviewResult, dateTimeProvider);

        flashcardReviewRepository.Add(review);
        await applicationDbContext.SaveChangesAsync(cancellationToken);

        review.Raise(new FlashcardReviewedDomainEvent(review.Id,command.FlaschardCollectionId, command.FlashcardId, review.ReviewDate, review.ReviewResult));

        return review.Id;
    }
}
