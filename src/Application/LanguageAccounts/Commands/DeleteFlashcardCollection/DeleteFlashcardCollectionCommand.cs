using Application.Abstractions.Messaging;

namespace Application.LanguageAccounts.Commands.DeleteFlashcardCollection;

public sealed record DeleteFlashcardCollectionCommand(Guid FlashcardCollectionId) : ICommand;
