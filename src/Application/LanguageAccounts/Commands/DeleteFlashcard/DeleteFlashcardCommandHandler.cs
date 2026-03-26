using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.LanguageAccount;
using Domain.Users;
using SharedKernel;

namespace Application.LanguageAccounts.Commands.DeleteFlashcard;

internal sealed class DeleteFlashcardCommandHandler(
    IFlashcardRepository flashcardRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<DeleteFlashcardCommand>
{
    public async Task<Result> Handle(DeleteFlashcardCommand command, CancellationToken cancellationToken)
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

        flashcardRepository.Remove(flashcard);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
