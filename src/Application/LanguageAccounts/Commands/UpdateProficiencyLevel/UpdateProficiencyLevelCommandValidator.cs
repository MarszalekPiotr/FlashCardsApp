using Domain.LanguageAccount.Enums;
using FluentValidation;

namespace Application.LanguageAccounts.Commands.UpdateProficiencyLevel;

internal sealed class UpdateProficiencyLevelCommandValidator : AbstractValidator<UpdateProficiencyLevelCommand>
{
    public UpdateProficiencyLevelCommandValidator()
    {
        RuleFor(c => c.LanguageAccountId).NotEmpty();
        RuleFor(c => c.ProficiencyLevel)
            .IsInEnum()
            .WithMessage("Proficiency level is not valid.");
    }
}
