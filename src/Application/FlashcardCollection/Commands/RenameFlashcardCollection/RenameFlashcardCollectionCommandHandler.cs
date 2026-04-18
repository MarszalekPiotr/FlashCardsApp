using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Authorization.FlashcardCollection;
using Domain.FlashcardCollection;
using Domain.Users;
using SharedKernel;

namespace Application.FlashcardCollection.Commands.RenameFlashcardCollection;

internal sealed class RenameFlashcardCollectionCommandHandler(
    IFlashcardCollectionRepository flashcardCollectionRepository,
    IApplicationDbContext applicationDbContext,
    IUserContext userContext,
    CanAccessFlashcardCollectionSpecification canAccessFlashcardCollectionSpecification)
    : ICommandHandler<RenameFlashcardCollectionCommand>
{
    public async Task<Result> Handle(RenameFlashcardCollectionCommand command, CancellationToken cancellationToken)
    {
        Domain.FlashcardCollection.FlashcardCollection? collection =
            await flashcardCollectionRepository.GetByIdAsync(command.FlashcardCollectionId, cancellationToken);

        if (collection is null)
        {
            return Result.Failure(FlashcardCollectionErrors.NotFound(command.FlashcardCollectionId));
        }

        bool canAccess = await canAccessFlashcardCollectionSpecification.IsSatisfiedByAsync(collection.Id, userContext.UserId, cancellationToken);

        if (!canAccess)
        {
            return Result.Failure(AuthorizationError.Forbidden());
        }

        collection.Rename(command.Name);
        await applicationDbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
