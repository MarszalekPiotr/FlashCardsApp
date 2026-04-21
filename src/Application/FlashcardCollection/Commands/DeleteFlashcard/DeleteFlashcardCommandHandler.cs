using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Authorization.FlashcardCollection;
using Domain.FlashcardCollection;
using SharedKernel;

namespace Application.FlashcardCollection.Commands.DeleteFlashcard;

internal sealed class DeleteFlashcardCommandHandler(
    IFlashcardRepository flashcardRepository,
    IApplicationDbContext applicationDbContext,
    IUserContext userContext,
    CanAccessFlashcardCollectionSpecification canAccessFlashcardCollectionSpecification)
    : ICommandHandler<DeleteFlashcardCommand>
{
    public async Task<Result> Handle(DeleteFlashcardCommand command, CancellationToken cancellationToken)
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

        flashcardRepository.Remove(flashcard);
        await applicationDbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
