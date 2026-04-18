using Domain.FlashcardCollection.Events;
using Domain.LanguageAccount.Events;
using SharedKernel;

namespace Domain.FlashcardCollection;

public class FlashcardCollection : Entity
{
    public Guid Id { get; private set; }
    public Guid LanguageAccountId { get; private set; }
    public string Name { get; private set; }

    private readonly List<Flashcard> _flashcards = new();
    public IReadOnlyCollection<Flashcard> Flashcards => _flashcards.AsReadOnly();

    private FlashcardCollection() { } // Required by EF Core

    private FlashcardCollection(Guid languageAccountId, string name)
    {
        LanguageAccountId = languageAccountId;
        Name = name;
  
    }

    public static FlashcardCollection Create(Guid languageAccountId, string name)
    {   
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

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
        return flashcard;
    }

    public void RemoveFlashcard(Guid flashcardId)
    {
        var flashcard = _flashcards.FirstOrDefault(f => f.Id == flashcardId);
        if (flashcard == null)
        {
            throw new ArgumentException("Flashcard not found.", nameof(flashcardId));
        }
        _flashcards.Remove(flashcard);
        //Raise(new FlashcardRemovedDomainEvent(flashcardId));
    }


    public void UpdateFlashcard(Guid flashcardId, string sentenceWithBlanks, string translation, string answer, Synonyms synonyms)
    {
        var flashcard = _flashcards.FirstOrDefault(f => f.Id == flashcardId);
        if (flashcard == null)
        {
            throw new ArgumentException("Flashcard not found.", nameof(flashcardId));
        }
        flashcard.Update(sentenceWithBlanks, translation, answer, synonyms);
        //Raise(new FlashcardUpdatedDomainEvent(flashcardId));
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
