using FluentValidation;

namespace Application.LanguageAccounts.Commands.RenameFlashcardCollection;

internal sealed class RenameFlashcardCollectionCommandValidator : AbstractValidator<RenameFlashcardCollectionCommand>
{
    public RenameFlashcardCollectionCommandValidator()
    {
        RuleFor(c => c.FlashcardCollectionId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
    }
}
