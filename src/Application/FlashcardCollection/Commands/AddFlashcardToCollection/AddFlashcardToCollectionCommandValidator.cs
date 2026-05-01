using FluentValidation;

namespace Application.FlashcardCollection.Commands.AddFlashcardToCollection;

internal sealed class AddFlashcardToCollectionCommandValidator : AbstractValidator<AddFlashcardToCollectionCommand>
{
    public AddFlashcardToCollectionCommandValidator()
    {
        RuleFor(c => c.FlashcardCollectionId).NotEmpty();
        RuleFor(c => c.SentenceWithBlanks).NotEmpty().MaximumLength(500);
        RuleFor(c => c.Translation).NotEmpty().MaximumLength(500);
        RuleFor(c => c.Answer).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Synonyms).NotNull();
    }
}
