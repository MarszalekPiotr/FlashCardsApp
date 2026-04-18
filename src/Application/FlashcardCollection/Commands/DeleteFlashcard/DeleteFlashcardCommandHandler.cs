using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Authorization.FlashcardCollection;
using Domain.FlashcardCollection;
using Domain.Users;
using SharedKernel;

namespace Application.FlashcardCollection.Commands.DeleteFlashcard;

internal sealed class DeleteFlashcardCommandHandler(
    IFlashcardCollectionRepository flashcardCollectionRepository,
    IApplicationDbContext applicationDbContext,
    IUserContext userContext,
    CanAccessFlashcardCollectionSpecification canAccessFlashcardCollectionSpecification)
    : ICommandHandler<DeleteFlashcardCommand>
{
    public async Task<Result> Handle(DeleteFlashcardCommand command, CancellationToken cancellationToken)
    {
        Domain.FlashcardCollection.FlashcardCollection? collection =
            await flashcardCollectionRepository.GetByIdWithSingleFlashcardAsync(command.FlashcardCollectionId, command.FlashcardId, cancellationToken);

        if (collection is null)
        {
            return Result.Failure(FlashcardCollectionErrors.NotFound(command.FlashcardId));
        }

        if(collection.Flashcards == null || !collection.Flashcards.Any())
        {
            return Result.Failure(FlashcardErrors.NotFound(command.FlashcardId));
        }

        var flashcard = collection.Flashcards.First();

        bool canAccess = await canAccessFlashcardCollectionSpecification.IsSatisfiedByAsync(collection.Id, userContext.UserId, cancellationToken);

        if (!canAccess)
        {
            return Result.Failure(AuthorizationError.Forbidden());
        }

        collection.RemoveFlashcard(flashcard.Id);
        await applicationDbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
