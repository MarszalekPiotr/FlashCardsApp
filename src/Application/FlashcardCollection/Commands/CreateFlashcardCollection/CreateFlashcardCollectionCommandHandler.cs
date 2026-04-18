using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.LanguageAccounts;
using Domain.FlashcardCollection;
using Domain.FlashcardCollection.Events;
using Domain.LanguageAccount;
using Domain.Users;
using SharedKernel;

namespace Application.FlashcardCollection.Commands.CreateFlashcardCollection;

internal sealed class CreateFlashcardCollectionCommandHandler(
    ILanguageAccountRepository languageAccountRepository,
    IApplicationDbContext applicationDbContext,
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

        Domain.FlashcardCollection.FlashcardCollection collection = Domain.FlashcardCollection.FlashcardCollection.Create(languageAccount.Id, command.Name);


        await applicationDbContext.SaveChangesAsync(cancellationToken);

        collection.Raise(new FlashcardCollectionCreatedDomainEvent(collection.Id));

        return collection.Id;
    }
}
