using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Authorization.FlashcardCollection;
using Domain.FlashcardCollection;
using Domain.LanguageAccount.ValueObjects;
using SharedKernel;

namespace Application.FlashcardCollection.Commands.UpdateFlashcard;

internal sealed class UpdateFlashcardCommandHandler(
    IFlashcardRepository flashcardRepository,
    IApplicationDbContext applicationDbContext,
    IUserContext userContext,
    CanAccessFlashcardCollectionSpecification canAccessFlashcardCollectionSpecification)
    : ICommandHandler<UpdateFlashcardCommand>
{
    public async Task<Result> Handle(UpdateFlashcardCommand command, CancellationToken cancellationToken)
    {
        Flashcard? flashcard = await flashcardRepository.GetByIdAsync(command.FlashcardId, cancellationToken);

        if (flashcard is null)
        {
            return Result.Failure(FlashcardErrors.NotFound(command.FlashcardId));
        }

        bool canAccess = await canAccessFlashcardCollectionSpecification.IsSatisfiedByAsync(flashcard.FlashcardCollectionId, userContext.UserId, cancellationToken);

        if (!canAccess)
        {
            return Result.Failure(AuthorizationError.Forbidden());
        }

        try
        {
            var synonyms = new Synonyms(command.Synonyms);
            flashcard.Update(command.SentenceWithBlanks, command.Translation, command.Answer, synonyms);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(Error.Problem("Flashcards.InvalidInput", ex.Message));
        }

        await applicationDbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
