using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.LanguageAccounts;
using Domain.LanguageAccount;
using Domain.Users;
using SharedKernel;

namespace Application.LanguageAccounts.Commands.DeleteFlashcardCollection;

internal sealed class DeleteFlashcardCollectionCommandHandler(
    IFlashcardCollectionRepository flashcardCollectionRepository,
    IApplicationDbContext applicationDbContext,
    IUserContext userContext)
    : ICommandHandler<DeleteFlashcardCollectionCommand>
{
    public async Task<Result> Handle(DeleteFlashcardCollectionCommand command, CancellationToken cancellationToken)
    {
        FlashcardCollection? collection =
            await flashcardCollectionRepository.GetByIdWithLanguageAccountAsync(command.FlashcardCollectionId, cancellationToken);

        if (collection is null)
        {
            return Result.Failure(FlashcardCollectionErrors.NotFound(command.FlashcardCollectionId));
        }

        if (collection.LanguageAccount!.UserId != userContext.UserId)
        {
            return Result.Failure(UserErrors.Unauthorized());
        }

        flashcardCollectionRepository.Remove(collection);
        await applicationDbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
