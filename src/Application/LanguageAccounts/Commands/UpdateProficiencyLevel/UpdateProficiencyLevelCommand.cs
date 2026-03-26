using Application.Abstractions.Messaging;

namespace Application.LanguageAccounts.Commands.UpdateProficiencyLevel;

public sealed record UpdateProficiencyLevelCommand(Guid LanguageAccountId, int ProficiencyLevel) : ICommand;
