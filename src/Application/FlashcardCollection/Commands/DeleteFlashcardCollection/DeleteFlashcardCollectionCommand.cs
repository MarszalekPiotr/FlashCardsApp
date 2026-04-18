using Application.Abstractions.Messaging;

namespace Application.FlashcardCollection.Commands.DeleteFlashcardCollection;

public sealed record DeleteFlashcardCollectionCommand(Guid FlashcardCollectionId) : ICommand;
