using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Authorization.LanguageAccount;
using Application.LanguageAccounts;
using Domain.LanguageAccount;
using Domain.Users;
using SharedKernel;

namespace Application.LanguageAccounts.Commands.DeleteLanguageAccount;

internal sealed class DeleteLanguageAccountCommandHandler(
    ILanguageAccountRepository languageAccountRepository,
    IApplicationDbContext applicationDbContext,
    CanAccessLanguageAccountSpecification canAccessLanguageAccountSpecification)
    : ICommandHandler<DeleteLanguageAccountCommand>
{
    public async Task<Result> Handle(DeleteLanguageAccountCommand command, CancellationToken cancellationToken)
    {
        Domain.LanguageAccount.LanguageAccount? account =
            await languageAccountRepository.GetByIdAsync(command.LanguageAccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(LanguageAccountErrors.NotFound(command.LanguageAccountId));
        }

        bool canAccess = await canAccessLanguageAccountSpecification.IsSatisfiedByAsync(account.Id, account.UserId, cancellationToken);
        if (!canAccess)
        {
            return Result.Failure(AuthorizationError.Forbidden());   
        }

        languageAccountRepository.Remove(account);
        await applicationDbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

