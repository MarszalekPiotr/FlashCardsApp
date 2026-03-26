using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.LanguageAccounts;
using Domain.LanguageAccount;
using Domain.Users;
using SharedKernel;

namespace Application.LanguageAccounts.Commands.DeleteLanguageAccount;

internal sealed class DeleteLanguageAccountCommandHandler(
    ILanguageAccountRepository languageAccountRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
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

        if (account.UserId != userContext.UserId)
        {
            return Result.Failure(UserErrors.Unauthorized());
        }

        languageAccountRepository.Remove(account);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
