using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.LanguageAccounts;
using Domain.LanguageAccount;
using Domain.LanguageAccount.Enums;
using Domain.LanguageAccount.ValueObjects;
using SharedKernel;

namespace Application.LanguageAccounts.Commands.CreateLanguageAccount;

internal sealed class CreateLanguageAccountCommandHandler(
    ILanguageAccountRepository languageAccountRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<CreateLanguageAccountCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateLanguageAccountCommand command, CancellationToken cancellationToken)
    {
        Language language = Language.GetSupportedLanguages()
            .First(l => l.Code == command.LanguageCode);

        var proficiencyLevel = new Domain.LanguageAccount.ValueObjects.ProficiencyLevel((Domain.LanguageAccount.Enums.ProficiencyLevel)command.ProficiencyLevel);

        var account = Domain.LanguageAccount.LanguageAccount.Create(
            userContext.UserId,
            proficiencyLevel,
            language);

        languageAccountRepository.Add(account);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}
