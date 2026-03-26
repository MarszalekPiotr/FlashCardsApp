using Domain.LanguageAccount.Enums;
using Domain.LanguageAccount.ValueObjects;
using FluentValidation;

namespace Application.LanguageAccounts.Commands.CreateLanguageAccount;

internal sealed class CreateLanguageAccountCommandValidator : AbstractValidator<CreateLanguageAccountCommand>
{
    public CreateLanguageAccountCommandValidator()
    {
        RuleFor(c => c.LanguageCode)
            .NotEmpty()
            .Must(code => Language.GetSupportedLanguages().Any(l => l.Code == code))
            .WithMessage("Language code is not supported.");

    }
}
