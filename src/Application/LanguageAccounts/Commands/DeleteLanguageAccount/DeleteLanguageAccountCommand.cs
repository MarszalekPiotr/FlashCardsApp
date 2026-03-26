using Application.Abstractions.Messaging;

namespace Application.LanguageAccounts.Commands.DeleteLanguageAccount;

public sealed record DeleteLanguageAccountCommand(Guid LanguageAccountId) : ICommand;
