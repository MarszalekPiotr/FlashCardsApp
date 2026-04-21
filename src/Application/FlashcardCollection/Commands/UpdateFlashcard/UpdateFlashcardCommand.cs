using Application.Abstractions.Messaging;

namespace Application.FlashcardCollection.Commands.UpdateFlashcard;

public sealed record UpdateFlashcardCommand(
    Guid FlashCardCollectionId,
    Guid FlashcardId,
    string SentenceWithBlanks,
    string Translation,
    string Answer,
    IEnumerable<string> Synonyms) : ICommand;
