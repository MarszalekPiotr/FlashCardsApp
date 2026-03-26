using Application.Abstractions.Messaging;

namespace Application.LanguageAccounts.Commands.UpdateFlashcard;

public sealed record UpdateFlashcardCommand(
    Guid FlashcardId,
    string SentenceWithBlanks,
    string Translation,
    string Answer,
    IEnumerable<string> Synonyms) : ICommand;
