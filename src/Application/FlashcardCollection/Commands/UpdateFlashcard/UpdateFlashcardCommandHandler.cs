using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.FlashcardCollection;
using Domain.FlashcardCollection;
using Domain.LanguageAccount.ValueObjects;
using Domain.Users;
using SharedKernel;

namespace Application.FlashcardCollection.Commands.UpdateFlashcard;

internal sealed class UpdateFlashcardCommandHandler(
    IFlashcardCollectionRepository flashcardCollectionRepository,
    IApplicationDbContext applicationDbContext,
    IUserContext userContext)
    : ICommandHandler<UpdateFlashcardCommand>
{
    public async Task<Result> Handle(UpdateFlashcardCommand command, CancellationToken cancellationToken)
    {
        Domain.FlashcardCollection.FlashcardCollection? collection =
            await flashcardCollectionRepository.GetByIdWithSingleFlashcardAsync(command.FlashCardCollectionId,command.FlashcardId, cancellationToken);

        if (collection is null)
        {
            return Result.Failure(FlashcardErrors.NotFound(command.FlashcardId));
        }

        if (collection.Flashcards == null || !collection.Flashcards.Any())
        {
            return Result.Failure(FlashcardErrors.NotFound(command.FlashcardId));
        }

        var flashcard = collection.Flashcards.First();

        if (collection.LanguageAccountId != userContext.UserId)
        {
            return Result.Failure(UserErrors.Unauthorized());
        }

        try
        {
            var synonyms = new Synonyms(command.Synonyms);
            collection.UpdateFlashcard(command.FlashcardId, command.SentenceWithBlanks, command.Translation, command.Answer, synonyms);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(Error.Problem("Flashcards.InvalidInput", ex.Message));
        }

        await applicationDbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
