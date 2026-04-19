using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Authorization.FlashcardCollection;
using Application.Authorization.LanguageAccount;
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
    IUserContext userContext,
    IFlashcardCollectionRepository flashcardCollectionRepository,
    CanAccessLanguageAccountSpecification canAccessLanguageAccountSpecification)
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

        bool canAccess = await canAccessLanguageAccountSpecification.IsSatisfiedByAsync(command.LanguageAccountId ,userContext.UserId, cancellationToken);
        if (!canAccess)
        {
            return Result.Failure<Guid>(AuthorizationError.Forbidden());
        }


        Domain.FlashcardCollection.FlashcardCollection collection = Domain.FlashcardCollection.FlashcardCollection.Create(languageAccount.Id, command.Name);

        await flashcardCollectionRepository.AddAsync(collection);

        await applicationDbContext.SaveChangesAsync(cancellationToken);

        collection.Raise(new FlashcardCollectionCreatedDomainEvent(collection.Id));

        return collection.Id;
    }
}
