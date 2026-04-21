using Application.Abstractions.Messaging;
using Domain.LanguageAccount.ValueObjects;

namespace Application.FlashcardCollection.Commands.AddFlashcardToCollection;

public sealed record AddFlashcardToCollectionCommand(
    Guid FlashcardCollectionId,
    string SentenceWithBlanks,
    string Translation,
    string Answer,
    IEnumerable<string> Synonyms) : ICommand<Guid>;
