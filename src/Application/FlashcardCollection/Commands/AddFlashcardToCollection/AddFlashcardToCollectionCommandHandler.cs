using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Authorization.FlashcardCollection;
using Domain.FlashcardCollection;
using Domain.FlashcardCollection.Events;
using Domain.LanguageAccount.ValueObjects;
using Domain.Users;
using SharedKernel;

namespace Application.FlashcardCollection.Commands.AddFlashcardToCollection;

internal sealed class AddFlashcardToCollectionCommandHandler(
    IFlashcardCollectionRepository flashcardCollectionRepository,
    IApplicationDbContext applicationDbContext,
    CanAccessFlashcardCollectionSpecification canAccessFlashcardCollectionSpecification,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AddFlashcardToCollectionCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddFlashcardToCollectionCommand command, CancellationToken cancellationToken)
    {
       Domain.FlashcardCollection.FlashcardCollection? collection =
            await flashcardCollectionRepository.GetByIdAsync(command.FlashcardCollectionId, cancellationToken);

        if (collection is null)
        {
            return Result.Failure<Guid>(FlashcardCollectionErrors.NotFound(command.FlashcardCollectionId));
        }

        bool canAccessCollection = await canAccessFlashcardCollectionSpecification.IsSatisfiedByAsync(
            collection.Id,
            userContext.UserId,
            cancellationToken);

        if (!canAccessCollection) {
            return Result.Failure<Guid>(AuthorizationError.Forbidden());
        }

        var synonyms = new Synonyms(command.Synonyms);
        Flashcard flashcard = collection.AddFlashcard(
            command.SentenceWithBlanks,
            command.Translation,
            command.Answer,
            synonyms,
            dateTimeProvider.UtcNow);
        flashcard.Raise(new FlashcardCreatedDomainEvent(flashcard.Id));

        await applicationDbContext.SaveChangesAsync(cancellationToken);

        return flashcard.Id;
    }
}
