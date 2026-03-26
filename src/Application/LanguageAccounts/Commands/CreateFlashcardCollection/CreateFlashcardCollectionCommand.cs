using Application.Abstractions.Messaging;

namespace Application.LanguageAccounts.Commands.CreateFlashcardCollection;

public sealed record CreateFlashcardCollectionCommand(Guid LanguageAccountId, string Name) : ICommand<Guid>;
