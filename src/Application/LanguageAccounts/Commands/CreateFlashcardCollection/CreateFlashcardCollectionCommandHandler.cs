using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.LanguageAccounts;
using Domain.LanguageAccount;
using Domain.Users;
using SharedKernel;

namespace Application.LanguageAccounts.Commands.CreateFlashcardCollection;

internal sealed class CreateFlashcardCollectionCommandHandler(
    ILanguageAccountRepository languageAccountRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<CreateFlashcardCollectionCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateFlashcardCollectionCommand command, CancellationToken cancellationToken)
    {
        Domain.LanguageAccount.LanguageAccount? languageAccount =
            await languageAccountRepository.GetByIdAsync(command.LanguageAccountId, cancellationToken);

        if (languageAccount is null)
        {
            return Result.Failure<Guid>(LanguageAccountErrors.NotFound(command.LanguageAccountId));
        }

        if (languageAccount.UserId != userContext.UserId)
        {
            return Result.Failure<Guid>(UserErrors.Unauthorized());
        }

        FlashcardCollection collection = languageAccount.CreateCollection(command.Name);


        await unitOfWork.SaveChangesAsync(cancellationToken);

        return collection.Id;
    }
}
