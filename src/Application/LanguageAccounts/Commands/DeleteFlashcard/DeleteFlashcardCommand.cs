using Application.Abstractions.Messaging;

namespace Application.LanguageAccounts.Commands.DeleteFlashcard;

public sealed record DeleteFlashcardCommand(Guid FlashcardId) : ICommand;
