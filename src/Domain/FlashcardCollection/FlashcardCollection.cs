using Domain.FlashcardCollection.Events;
using SharedKernel;

namespace Domain.FlashcardCollection;

public class FlashcardCollection : Entity
{
    public Guid Id { get; private set; }
    public Guid LanguageAccountId { get; private set; }
    public string Name { get; private set; }

    private FlashcardCollection() { } // Required by EF Core — must NOT raise events

    private FlashcardCollection(Guid languageAccountId, string name)
    {
        Id = Guid.NewGuid();
        LanguageAccountId = languageAccountId;
        Name = name;

        // Domain guarantees: a FlashcardCollectionCreatedDomainEvent is always raised
        // when a collection is created, regardless of which handler calls Create().
        Raise(new FlashcardCollectionCreatedDomainEvent(Id));
    }

    public static FlashcardCollection Create(Guid languageAccountId, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        return new FlashcardCollection(languageAccountId, name);
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
