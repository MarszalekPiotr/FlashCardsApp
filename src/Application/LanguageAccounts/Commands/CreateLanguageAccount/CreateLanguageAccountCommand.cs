using Application.Abstractions.Messaging;

namespace Application.LanguageAccounts.Commands.CreateLanguageAccount;

public sealed record CreateLanguageAccountCommand(string LanguageCode, int ProficiencyLevel) : ICommand<Guid>;
