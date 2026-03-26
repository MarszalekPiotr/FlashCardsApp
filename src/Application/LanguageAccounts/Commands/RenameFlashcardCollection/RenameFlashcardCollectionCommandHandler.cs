using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.LanguageAccounts;
using Domain.LanguageAccount;
using Domain.Users;
using SharedKernel;

namespace Application.LanguageAccounts.Commands.RenameFlashcardCollection;

internal sealed class RenameFlashcardCollectionCommandHandler(
    IFlashcardCollectionRepository flashcardCollectionRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<RenameFlashcardCollectionCommand>
{
    public async Task<Result> Handle(RenameFlashcardCollectionCommand command, CancellationToken cancellationToken)
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

        collection.Rename(command.Name);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
