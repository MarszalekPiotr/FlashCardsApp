using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.LanguageAccounts;
using Domain.LanguageAccount;
using Domain.LanguageAccount.ValueObjects;
using Domain.Users;
using SharedKernel;

namespace Application.LanguageAccounts.Commands.UpdateProficiencyLevel;

internal sealed class UpdateProficiencyLevelCommandHandler(
    ILanguageAccountRepository languageAccountRepository,
    IApplicationDbContext applicationDbContext,
    IUserContext userContext)
    : ICommandHandler<UpdateProficiencyLevelCommand>
{
    public async Task<Result> Handle(UpdateProficiencyLevelCommand command, CancellationToken cancellationToken)
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

        var newLevel = new ProficiencyLevel((Domain.LanguageAccount.Enums.ProficiencyLevel)command.ProficiencyLevel);

        try
        {
            account.UpdateProficiencyLevel(newLevel);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(Error.Problem("LanguageAccounts.ProficiencyDowngrade", ex.Message));
        }

        await applicationDbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
