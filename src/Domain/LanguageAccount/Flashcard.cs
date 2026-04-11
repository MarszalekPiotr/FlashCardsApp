using Domain.LanguageAccount.ValueObjects;
using SharedKernel;

namespace Domain.LanguageAccount;

public class Flashcard : Entity
{
    public Guid Id { get; private set; }
    public Guid FlashcardCollectionId { get; private set; }
    public FlashcardCollection? FlashcardCollection { get; private set; }

    public string SentenceWithBlanks { get; private set; }
    public string Translation { get; private set; }
    public string Answer { get; private set; }
    public Synonyms Synonyms { get; private set; }

    private Flashcard() { } // Required by EF Core

    internal Flashcard(Guid flashcardCollectionId, string sentenceWithBlanks, string translation, string answer, Synonyms synonyms)
    {
        FlashcardCollectionId = flashcardCollectionId;
        SentenceWithBlanks = sentenceWithBlanks;
        Translation = translation;
        Answer = answer;
        Synonyms = synonyms;
    }

    public void Update(string sentenceWithBlanks, string translation, string answer, Synonyms synonyms)
    {
        if (string.IsNullOrWhiteSpace(sentenceWithBlanks))
        {
            throw new ArgumentException("Sentence with blanks cannot be null or whitespace.", nameof(sentenceWithBlanks));
        }

        if (string.IsNullOrWhiteSpace(translation))
        {
            throw new ArgumentException("Translation cannot be null or whitespace.", nameof(translation));
        }

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new ArgumentException("Answer cannot be null or whitespace.", nameof(answer));
        }

        ArgumentNullException.ThrowIfNull(synonyms);

        SentenceWithBlanks = sentenceWithBlanks;
        Translation = translation;
        Answer = answer;
        Synonyms = synonyms;
    }
}
