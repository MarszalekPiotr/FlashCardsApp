using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.LanguageAccounts;
using Application.Shared;
using Application.Shared.DTO;
using Domain.LanguageAccount;
using Domain.LanguageAccount.Enums;
using Domain.LanguageAccount.Events;
using Domain.LanguageAccount.ValueObjects;
using SharedKernel;
using SharedKernel.SharedEntities.Language;

namespace Application.LanguageAccounts.Commands.CreateLanguageAccount;

internal sealed class CreateLanguageAccountCommandHandler(
    ILanguageAccountRepository languageAccountRepository,
    IApplicationDbContext applicationDbContext,
    IUserContext userContext,
    ILanguageReadRepository languageReadRepository)
    : ICommandHandler<CreateLanguageAccountCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateLanguageAccountCommand command, CancellationToken cancellationToken)
    {
         IReadOnlyCollection<LanguageDetailReadModel> languageDetailReadModels = await languageReadRepository.GetActiveLanguagesAsync();

        var language = languageDetailReadModels.FirstOrDefault(l => l.Code == command.LanguageCode);

        if (language == null)
        {
            return Result.Failure<Guid>(LanguageErrors.CodeNotAvailable(command.LanguageCode));
        }

        var proficiencyLevel = new Domain.LanguageAccount.ValueObjects.ProficiencyLevel((Domain.LanguageAccount.Enums.ProficiencyLevel)command.ProficiencyLevel);

        var account = Domain.LanguageAccount.LanguageAccount.Create(
            userContext.UserId,
            proficiencyLevel,
            language.Id);

        languageAccountRepository.Add(account);
        await applicationDbContext.SaveChangesAsync(cancellationToken);

        account.Raise(new LanguageAccountCreatedDomainEvent(account.Id));

        return account.Id;
    }
}
