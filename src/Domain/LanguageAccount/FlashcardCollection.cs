using Domain.LanguageAccount.Events;
using Domain.LanguageAccount.ValueObjects;
using SharedKernel;

namespace Domain.LanguageAccount;

public class FlashcardCollection : Entity
{
    public Guid Id { get; private set; }
    public Guid LanguageAccountId { get; private set; }
    public LanguageAccount? LanguageAccount { get; private set; }
    public string Name { get; private set; }

    private readonly List<Flashcard> _flashcards = new();
    public IReadOnlyCollection<Flashcard> Flashcards => _flashcards.AsReadOnly();

    private FlashcardCollection() { } // Required by EF Core

    private FlashcardCollection(Guid languageAccountId, string name)
    {
        // Id = Guid.NewGuid();
        LanguageAccountId = languageAccountId;
        Name = name;
        Raise(new FlashcardCollectionCreatedDomainEvent(Id));
    }

    internal static FlashcardCollection Create(Guid languageAccountId, string name)
    {
        return new FlashcardCollection(languageAccountId, name);
    }

    public Flashcard AddFlashcard(string sentenceWithBlanks, string translation, string answer, Synonyms synonyms)
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

        var flashcard = new Flashcard(Id, sentenceWithBlanks, translation, answer, synonyms);
        _flashcards.Add(flashcard);
        Raise(new FlashcardCreatedDomainEvent(flashcard.Id));
        return flashcard;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
        }

        Name = name;
    }
}
