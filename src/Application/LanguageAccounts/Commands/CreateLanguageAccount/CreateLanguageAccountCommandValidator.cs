using Domain.LanguageAccount.Enums;
using Domain.LanguageAccount.ValueObjects;
using FluentValidation;

namespace Application.LanguageAccounts.Commands.CreateLanguageAccount;

internal sealed class CreateLanguageAccountCommandValidator : AbstractValidator<CreateLanguageAccountCommand>
{
    public CreateLanguageAccountCommandValidator()
    {
      

    }
}
