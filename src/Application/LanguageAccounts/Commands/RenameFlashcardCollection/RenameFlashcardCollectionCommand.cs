using Application.Abstractions.Messaging;

namespace Application.LanguageAccounts.Commands.RenameFlashcardCollection;

public sealed record RenameFlashcardCollectionCommand(Guid FlashcardCollectionId, string Name) : ICommand;
