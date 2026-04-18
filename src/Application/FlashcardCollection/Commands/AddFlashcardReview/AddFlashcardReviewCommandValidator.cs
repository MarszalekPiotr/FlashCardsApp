using Domain.FlashcardCollection.Enums;
using FluentValidation;

namespace Application.FlashcardCollection.Commands.AddFlashcardReview;

internal sealed class AddFlashcardReviewCommandValidator : AbstractValidator<AddFlashcardReviewCommand>
{
    public AddFlashcardReviewCommandValidator()
    {
        RuleFor(c => c.FlashcardId).NotEmpty();

        RuleFor(c => c.ReviewResult)
            .Must(r => Enum.IsDefined(typeof(ReviewResult), r))
            .WithMessage("Review result is not valid.");
    }
}
