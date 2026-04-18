using Application.Abstractions.Messaging;

namespace Application.FlashcardCollection.Commands.RenameFlashcardCollection;

public sealed record RenameFlashcardCollectionCommand(Guid FlashcardCollectionId, string Name) : ICommand;
