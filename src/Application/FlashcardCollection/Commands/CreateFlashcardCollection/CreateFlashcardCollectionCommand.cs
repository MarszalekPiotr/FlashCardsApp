using Application.Abstractions.Messaging;

namespace Application.FlashcardCollection.Commands.CreateFlashcardCollection;

public sealed record CreateFlashcardCollectionCommand(Guid LanguageAccountId, string Name) : ICommand<Guid>;
