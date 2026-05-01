using Application.Abstractions.Messaging;

namespace Application.FlashcardCollection.Commands.DeleteFlashcard;

public sealed record DeleteFlashcardCommand(Guid FlashcardCollectionId, Guid FlashcardId) : ICommand;
