using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.LanguageAccount;
using Domain.LanguageAccount.ValueObjects;
using Domain.Users;
using SharedKernel;

namespace Application.LanguageAccounts.Commands.UpdateFlashcard;

internal sealed class UpdateFlashcardCommandHandler(
    IFlashcardRepository flashcardRepository,
    IApplicationDbContext applicationDbContext,
    IUserContext userContext)
    : ICommandHandler<UpdateFlashcardCommand>
{
    public async Task<Result> Handle(UpdateFlashcardCommand command, CancellationToken cancellationToken)
    {
        Flashcard? flashcard =
            await flashcardRepository.GetByIdWithCollectionAsync(command.FlashcardId, cancellationToken);

        if (flashcard is null)
        {
            return Result.Failure(FlashcardErrors.NotFound(command.FlashcardId));
        }

        if (flashcard.FlashcardCollection!.LanguageAccount!.UserId != userContext.UserId)
        {
            return Result.Failure(UserErrors.Unauthorized());
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
