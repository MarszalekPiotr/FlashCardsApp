using FluentValidation;

namespace Application.FlashcardCollection.Commands.UpdateFlashcard;

internal sealed class UpdateFlashcardCommandValidator : AbstractValidator<UpdateFlashcardCommand>
{
    public UpdateFlashcardCommandValidator()
    {
        RuleFor(c => c.FlashcardId).NotEmpty();
        RuleFor(c => c.SentenceWithBlanks).NotEmpty().MaximumLength(500);
        RuleFor(c => c.Translation).NotEmpty().MaximumLength(500);
        RuleFor(c => c.Answer).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Synonyms).NotNull();
    }
}
