using FluentValidation;

namespace Application.LanguageAccounts.Commands.CreateFlashcardCollection;

internal sealed class CreateFlashcardCollectionCommandValidator : AbstractValidator<CreateFlashcardCollectionCommand>
{
    public CreateFlashcardCollectionCommandValidator()
    {
        RuleFor(c => c.LanguageAccountId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
    }
}
