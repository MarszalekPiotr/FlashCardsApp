using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.SRS;
using Domain.LanguageAccount;
using Domain.SRS.ValueObjects;
using Domain.Users;
using SharedKernel;

namespace Application.LanguageAccounts.Commands.AddFlashcardReview;

internal sealed class AddFlashcardReviewCommandHandler(
    IFlashcardRepository flashcardRepository,
    IFlashcardReviewRepository flashcardReviewRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<AddFlashcardReviewCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddFlashcardReviewCommand command, CancellationToken cancellationToken)
    {
        Flashcard? flashcard = await flashcardRepository.GetByIdWithCollectionAsync(command.FlashcardId, cancellationToken);

        if (flashcard is null)
            return Result.Failure<Guid>(FlashcardErrors.NotFound(command.FlashcardId));

        if (flashcard.FlashcardCollection!.LanguageAccount!.UserId != userContext.UserId)
            return Result.Failure<Guid>(UserErrors.Unauthorized());

        var reviewResult = new ReviewResult((Domain.SRS.Enums.ReviewResult)command.ReviewResult);
        var review = Domain.SRS.FlashcardReview.Create(command.FlashcardId, DateTime.UtcNow, reviewResult);

        flashcardReviewRepository.Add(review);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return review.Id;
    }
}
