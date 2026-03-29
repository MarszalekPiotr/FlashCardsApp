using FluentValidation;

namespace Application.LanguageAccounts.Commands.AddFlashcardReview;

internal sealed class AddFlashcardReviewCommandValidator : AbstractValidator<AddFlashcardReviewCommand>
{
    public AddFlashcardReviewCommandValidator()
    {
        RuleFor(c => c.FlashcardId).NotEmpty();

        RuleFor(c => c.ReviewResult)
            .Must(r => Enum.IsDefined(typeof(Domain.LanguageAccount.Enums.ReviewResult), r))
            .WithMessage("Review result is not valid.");
    }
}
