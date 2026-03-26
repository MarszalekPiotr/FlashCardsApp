using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.LanguageAccounts;
using Domain.LanguageAccount;
using Domain.LanguageAccount.ValueObjects;
using Domain.Users;
using SharedKernel;

namespace Application.LanguageAccounts.Commands.AddFlashcardToCollection;

internal sealed class AddFlashcardToCollectionCommandHandler(
    IFlashcardCollectionRepository flashcardCollectionRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<AddFlashcardToCollectionCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddFlashcardToCollectionCommand command, CancellationToken cancellationToken)
    {
        FlashcardCollection? collection =
            await flashcardCollectionRepository.GetByIdWithLanguageAccountAsync(command.FlashcardCollectionId, cancellationToken);

        if (collection is null)
        {
            return Result.Failure<Guid>(FlashcardCollectionErrors.NotFound(command.FlashcardCollectionId));
        }

        if (collection.LanguageAccount!.UserId != userContext.UserId)
        {
            return Result.Failure<Guid>(UserErrors.Unauthorized());
        }

        var synonyms = new Synonyms(command.Synonyms);
        Flashcard flashcard = collection.AddFlashcard(
            command.SentenceWithBlanks,
            command.Translation,
            command.Answer,
            synonyms);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return flashcard.Id;
    }
}
